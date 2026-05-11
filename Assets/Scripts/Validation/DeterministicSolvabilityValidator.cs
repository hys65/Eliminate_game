using System.Collections.Generic;
using System.Linq;
using System.Text;
using EliminateGame.Data;
using EliminateGame.Pattern;
using UnityEngine;

namespace EliminateGame.Validation
{
    public static class DeterministicSolvabilityValidator
    {
        private const int MaxSearchNodes = 200000;

        public static bool Validate(GameConfig config, string context)
        {
            if (config == null)
            {
                Debug.LogError($"[SOLVABILITY_VALIDATION][{context}] FAILED: GameConfig is null.");
                return false;
            }

            ValidationSnapshot snapshot = ValidationSnapshot.FromConfig(config);
            List<string> errors = new List<string>();

            ValidateColorCounts(snapshot, errors);
            ValidateInitialUnlocks(snapshot, errors);
            ValidateReachability(snapshot, errors);
            ValidateDeterministicPlayableSequence(snapshot, errors);

            if (errors.Count > 0)
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine($"[SOLVABILITY_VALIDATION][{context}] FAILED");
                for (int i = 0; i < errors.Count; i++)
                {
                    builder.AppendLine($"- {errors[i]}");
                }

                Debug.LogError(builder.ToString());
                return false;
            }

            Debug.Log($"[SOLVABILITY_VALIDATION][{context}] PASSED: Pattern colors, SelectionArea reachability, endgame color availability, and deterministic sequence solvability are valid.");
            return true;
        }

        private static void ValidateColorCounts(ValidationSnapshot snapshot, List<string> errors)
        {
            HashSet<BlockColor> colors = new HashSet<BlockColor>(snapshot.PatternCounts.Keys);
            colors.UnionWith(snapshot.SelectionCounts.Keys);

            foreach (BlockColor color in colors.OrderBy(c => (int)c))
            {
                if (color == BlockColor.None)
                {
                    continue;
                }

                int patternCount = snapshot.PatternCounts.GetValueOrDefault(color, 0);
                int selectionCount = snapshot.SelectionCounts.GetValueOrDefault(color, 0);
                int expectedPatternCount = selectionCount * 3;

                if (patternCount != expectedPatternCount)
                {
                    errors.Add($"Color count mismatch for {color}: PatternCount={patternCount}, SelectionCount={selectionCount}, Expected PatternCount=SelectionCount*3={expectedPatternCount}.");
                }
            }
        }

        private static void ValidateInitialUnlocks(ValidationSnapshot snapshot, List<string> errors)
        {
            int initialUnlockedCount = snapshot.SelectionTiles.Count(tile => tile.StartUnlocked && tile.Color != BlockColor.None);
            if (initialUnlockedCount <= 0)
            {
                errors.Add("SelectionArea has no initially unlocked non-None tile. First move is impossible.");
            }
        }

        private static void ValidateReachability(ValidationSnapshot snapshot, List<string> errors)
        {
            Dictionary<Vector2Int, SelectionNode> nodes = snapshot.SelectionNodes;
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

            foreach (SelectionNode node in nodes.Values)
            {
                if (!node.StartUnlocked)
                {
                    continue;
                }

                if (visited.Add(node.Position))
                {
                    queue.Enqueue(node.Position);
                }
            }

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                foreach (Vector2Int neighbor in GetOrthogonalNeighbors(current))
                {
                    if (!nodes.ContainsKey(neighbor))
                    {
                        continue;
                    }

                    if (visited.Add(neighbor))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            foreach (SelectionNode node in nodes.Values.OrderBy(n => n.Position.y).ThenBy(n => n.Position.x))
            {
                if (!visited.Contains(node.Position))
                {
                    errors.Add($"SelectionArea tile unreachable by orthogonal unlock path: position=({node.Position.x},{node.Position.y}), color={node.Color}.");
                }
            }
        }

        private static void ValidateDeterministicPlayableSequence(ValidationSnapshot snapshot, List<string> errors)
        {
            SearchState initialState = SearchState.FromSnapshot(snapshot);
            Queue<SearchState> open = new Queue<SearchState>();
            HashSet<string> visited = new HashSet<string>();

            open.Enqueue(initialState);
            visited.Add(initialState.BuildKey());

            int exploredNodes = 0;
            string bestFailure = "No playable sequence found.";

            while (open.Count > 0)
            {
                SearchState state = open.Dequeue();
                exploredNodes++;

                if (state.Pattern.IsEmpty())
                {
                    Debug.Log($"[SOLVABILITY_VALIDATION] Solvable sequence found. ExploredNodes={exploredNodes}, Sequence=[{string.Join(" -> ", state.MoveLog)}]");
                    return;
                }

                if (exploredNodes > MaxSearchNodes)
                {
                    errors.Add($"Solvability search exceeded MaxSearchNodes={MaxSearchNodes}. This level may be too complex for deterministic validation.");
                    return;
                }

                List<SelectionNode> candidates = state.GetUnlockedRemainingTiles();
                if (candidates.Count == 0)
                {
                    bestFailure = "No unlocked SelectionArea tile remains before Pattern is empty.";
                    continue;
                }

                bool expanded = false;
                foreach (SelectionNode candidate in candidates)
                {
                    SearchState next = state.Clone();
                    PlayCandidate(next, candidate);

                    if (next.IsLoseState())
                    {
                        bestFailure = $"Unavoidable deadlock candidate reached after selecting ({candidate.Position.x},{candidate.Position.y}) {candidate.Color}: TempZone full and no TempZone color matches current Pattern bottom row.";
                        continue;
                    }

                    string key = next.BuildKey();
                    if (!visited.Add(key))
                    {
                        continue;
                    }

                    expanded = true;
                    open.Enqueue(next);
                }

                if (!expanded && candidates.Count > 0)
                {
                    bestFailure = "All currently reachable branches repeat or lead to deadlock before Pattern is empty.";
                }
            }

            errors.Add($"Playable sequence solvability failed. {bestFailure}");
        }

        private static void PlayCandidate(SearchState state, SelectionNode candidate)
        {
            state.RemovedSelectionPositions.Add(candidate.Position);
            state.TempSlots.Add(new TempSlot(candidate.Color, 0));
            state.UnlockOrthogonalNeighbors(candidate.Position);
            state.MoveLog.Add($"({candidate.Position.x},{candidate.Position.y}) {candidate.Color}");

            ResolveColorChain(state, candidate.Color);
            ResolveAutomaticChain(state);
            CleanupStaleTempSlots(state);
        }

        private static void ResolveColorChain(SearchState state, BlockColor color)
        {
            int slotIndex = state.FindFirstTempSlotIndexByColor(color);
            if (slotIndex < 0)
            {
                return;
            }

            ResolveAgainstTempSlot(state, color, slotIndex);
        }

        private static void ResolveAutomaticChain(SearchState state)
        {
            int safety = 0;
            while (safety < 128)
            {
                safety++;
                HashSet<BlockColor> bottomColors = new HashSet<BlockColor>(state.Pattern.GetBottomRowColors());
                if (bottomColors.Count == 0)
                {
                    return;
                }

                int slotIndex = -1;
                BlockColor color = BlockColor.None;
                for (int i = 0; i < state.TempSlots.Count; i++)
                {
                    if (bottomColors.Contains(state.TempSlots[i].Color))
                    {
                        slotIndex = i;
                        color = state.TempSlots[i].Color;
                        break;
                    }
                }

                if (slotIndex < 0)
                {
                    return;
                }

                if (!ResolveAgainstTempSlot(state, color, slotIndex))
                {
                    return;
                }
            }
        }

        private static bool ResolveAgainstTempSlot(SearchState state, BlockColor color, int tempSlotIndex)
        {
            int bottomRowCount = state.Pattern.GetBottomRowCount(color);
            if (bottomRowCount <= 0)
            {
                return false;
            }

            int sameColorTempCount = state.TempSlots.Count(slot => slot.Color == color);
            int removedFromPattern = state.Pattern.ResolveAgainstBottomRow(color);
            if (removedFromPattern <= 0)
            {
                return false;
            }

            bool caseB = bottomRowCount >= 3 && sameColorTempCount >= 3;
            if (caseB)
            {
                int removed = 0;
                for (int i = state.TempSlots.Count - 1; i >= 0 && removed < 3; i--)
                {
                    if (state.TempSlots[i].Color != color)
                    {
                        continue;
                    }

                    state.TempSlots.RemoveAt(i);
                    removed++;
                }
            }
            else
            {
                if (tempSlotIndex >= 0 && tempSlotIndex < state.TempSlots.Count)
                {
                    TempSlot slot = state.TempSlots[tempSlotIndex];
                    slot.Progress = Mathf.Clamp(slot.Progress + removedFromPattern, 0, 3);
                    if (slot.Progress >= 3)
                    {
                        state.TempSlots.RemoveAt(tempSlotIndex);
                    }
                    else
                    {
                        state.TempSlots[tempSlotIndex] = slot;
                    }
                }
            }

            CleanupStaleTempSlots(state);
            return true;
        }

        private static void CleanupStaleTempSlots(SearchState state)
        {
            for (int i = state.TempSlots.Count - 1; i >= 0; i--)
            {
                if (!state.Pattern.ContainsColor(state.TempSlots[i].Color))
                {
                    state.TempSlots.RemoveAt(i);
                }
            }
        }

        private static IEnumerable<Vector2Int> GetOrthogonalNeighbors(Vector2Int position)
        {
            yield return new Vector2Int(position.x + 1, position.y);
            yield return new Vector2Int(position.x - 1, position.y);
            yield return new Vector2Int(position.x, position.y + 1);
            yield return new Vector2Int(position.x, position.y - 1);
        }

        private struct TempSlot
        {
            public readonly BlockColor Color;
            public int Progress { get; set; }

            public TempSlot(BlockColor color, int progress)
            {
                Color = color;
                Progress = progress;
            }
        }

        private sealed class SelectionNode
        {
            public Vector2Int Position;
            public BlockColor Color;
            public bool StartUnlocked;
        }

        private sealed class ValidationSnapshot
        {
            public readonly List<GameConfig.SelectionTileDefinition> SelectionTiles = new List<GameConfig.SelectionTileDefinition>();
            public readonly Dictionary<Vector2Int, SelectionNode> SelectionNodes = new Dictionary<Vector2Int, SelectionNode>();
            public readonly Dictionary<BlockColor, int> PatternCounts = new Dictionary<BlockColor, int>();
            public readonly Dictionary<BlockColor, int> SelectionCounts = new Dictionary<BlockColor, int>();
            public readonly List<List<BlockColor>> PatternRows = new List<List<BlockColor>>();
            public int TempZoneCapacity;

            public static ValidationSnapshot FromConfig(GameConfig config)
            {
                ValidationSnapshot snapshot = new ValidationSnapshot();
                snapshot.TempZoneCapacity = config.TempZoneCapacity;

                foreach (GameConfig.PatternRowDefinition row in config.PatternRows)
                {
                    List<BlockColor> copiedRow = new List<BlockColor>();
                    foreach (BlockColor color in row.Cells)
                    {
                        copiedRow.Add(color);
                        if (color != BlockColor.None)
                        {
                            snapshot.PatternCounts[color] = snapshot.PatternCounts.GetValueOrDefault(color, 0) + 1;
                        }
                    }

                    snapshot.PatternRows.Add(copiedRow);
                }

                HashSet<Vector2Int> seen = new HashSet<Vector2Int>();
                foreach (GameConfig.SelectionTileDefinition tile in config.SelectionTiles)
                {
                    if (tile.Color == BlockColor.None)
                    {
                        continue;
                    }

                    Vector2Int position = new Vector2Int(tile.X, tile.Y);
                    if (!seen.Add(position))
                    {
                        continue;
                    }

                    snapshot.SelectionTiles.Add(tile);
                    snapshot.SelectionCounts[tile.Color] = snapshot.SelectionCounts.GetValueOrDefault(tile.Color, 0) + 1;
                    snapshot.SelectionNodes[position] = new SelectionNode
                    {
                        Position = position,
                        Color = tile.Color,
                        StartUnlocked = tile.StartUnlocked
                    };
                }

                return snapshot;
            }
        }

        private sealed class SearchState
        {
            public PatternState Pattern;
            public readonly Dictionary<Vector2Int, SelectionNode> SelectionNodes = new Dictionary<Vector2Int, SelectionNode>();
            public readonly HashSet<Vector2Int> RemovedSelectionPositions = new HashSet<Vector2Int>();
            public readonly HashSet<Vector2Int> UnlockedPositions = new HashSet<Vector2Int>();
            public readonly List<TempSlot> TempSlots = new List<TempSlot>();
            public readonly List<string> MoveLog = new List<string>();
            public int TempZoneCapacity;

            public static SearchState FromSnapshot(ValidationSnapshot snapshot)
            {
                SearchState state = new SearchState
                {
                    Pattern = new PatternState(snapshot.PatternRows),
                    TempZoneCapacity = snapshot.TempZoneCapacity
                };

                foreach (KeyValuePair<Vector2Int, SelectionNode> pair in snapshot.SelectionNodes)
                {
                    state.SelectionNodes[pair.Key] = pair.Value;
                    if (pair.Value.StartUnlocked)
                    {
                        state.UnlockedPositions.Add(pair.Key);
                    }
                }

                return state;
            }

            public SearchState Clone()
            {
                SearchState clone = new SearchState
                {
                    Pattern = Pattern.Clone(),
                    TempZoneCapacity = TempZoneCapacity
                };

                foreach (KeyValuePair<Vector2Int, SelectionNode> pair in SelectionNodes)
                {
                    clone.SelectionNodes[pair.Key] = pair.Value;
                }

                foreach (Vector2Int position in RemovedSelectionPositions)
                {
                    clone.RemovedSelectionPositions.Add(position);
                }

                foreach (Vector2Int position in UnlockedPositions)
                {
                    clone.UnlockedPositions.Add(position);
                }

                clone.TempSlots.AddRange(TempSlots);
                clone.MoveLog.AddRange(MoveLog);
                return clone;
            }

            public List<SelectionNode> GetUnlockedRemainingTiles()
            {
                List<SelectionNode> result = new List<SelectionNode>();
                foreach (Vector2Int position in UnlockedPositions.OrderBy(p => p.y).ThenBy(p => p.x))
                {
                    if (RemovedSelectionPositions.Contains(position))
                    {
                        continue;
                    }

                    if (SelectionNodes.TryGetValue(position, out SelectionNode node))
                    {
                        result.Add(node);
                    }
                }

                return result;
            }

            public void UnlockOrthogonalNeighbors(Vector2Int position)
            {
                foreach (Vector2Int neighbor in GetOrthogonalNeighbors(position))
                {
                    if (SelectionNodes.ContainsKey(neighbor))
                    {
                        UnlockedPositions.Add(neighbor);
                    }
                }
            }

            public int FindFirstTempSlotIndexByColor(BlockColor color)
            {
                for (int i = 0; i < TempSlots.Count; i++)
                {
                    if (TempSlots[i].Color == color)
                    {
                        return i;
                    }
                }

                return -1;
            }

            public bool IsLoseState()
            {
                if (Pattern.IsEmpty())
                {
                    return false;
                }

                if (TempSlots.Count < TempZoneCapacity)
                {
                    return false;
                }

                HashSet<BlockColor> bottomColors = new HashSet<BlockColor>(Pattern.GetBottomRowColors());
                return !TempSlots.Any(slot => bottomColors.Contains(slot.Color));
            }

            public string BuildKey()
            {
                string patternKey = Pattern.BuildKey();
                string removedKey = string.Join(";", RemovedSelectionPositions.OrderBy(p => p.y).ThenBy(p => p.x).Select(p => $"{p.x},{p.y}"));
                string unlockedKey = string.Join(";", UnlockedPositions.OrderBy(p => p.y).ThenBy(p => p.x).Select(p => $"{p.x},{p.y}"));
                string tempKey = string.Join(";", TempSlots.Select(slot => $"{(int)slot.Color}:{slot.Progress}"));
                return $"P={patternKey}|R={removedKey}|U={unlockedKey}|T={tempKey}";
            }
        }

        private sealed class PatternState
        {
            private readonly List<List<BlockColor>> rows;

            public PatternState(List<List<BlockColor>> sourceRows)
            {
                rows = sourceRows.Select(row => new List<BlockColor>(row)).ToList();
            }

            public PatternState Clone()
            {
                return new PatternState(rows);
            }

            public bool IsEmpty()
            {
                return GetBottomRowIndex() < 0;
            }

            public bool ContainsColor(BlockColor color)
            {
                if (color == BlockColor.None)
                {
                    return false;
                }

                return rows.Any(row => row.Any(cell => cell == color));
            }

            public List<BlockColor> GetBottomRowColors()
            {
                int bottomIndex = GetBottomRowIndex();
                if (bottomIndex < 0)
                {
                    return new List<BlockColor>();
                }

                return rows[bottomIndex].Where(color => color != BlockColor.None).ToList();
            }

            public int GetBottomRowCount(BlockColor color)
            {
                int bottomIndex = GetBottomRowIndex();
                if (bottomIndex < 0)
                {
                    return 0;
                }

                return rows[bottomIndex].Count(cell => cell == color);
            }

            public int ResolveAgainstBottomRow(BlockColor color)
            {
                int bottomIndex = GetBottomRowIndex();
                if (bottomIndex < 0)
                {
                    return 0;
                }

                List<BlockColor> bottomRow = rows[bottomIndex];
                List<int> indices = new List<int>();
                for (int i = 0; i < bottomRow.Count; i++)
                {
                    if (bottomRow[i] == color)
                    {
                        indices.Add(i);
                    }
                }

                if (indices.Count <= 0)
                {
                    return 0;
                }

                int removeCount = indices.Count < 3 ? indices.Count : 3;
                for (int i = 0; i < removeCount; i++)
                {
                    bottomRow[indices[i]] = BlockColor.None;
                }

                ApplyColumnGravity();
                CollapseIfNeeded();
                return removeCount;
            }

            public string BuildKey()
            {
                return string.Join("/", rows.Select(row => string.Join(",", row.Select(color => ((int)color).ToString()))));
            }

            private int GetBottomRowIndex()
            {
                for (int rowIndex = rows.Count - 1; rowIndex >= 0; rowIndex--)
                {
                    if (rows[rowIndex].Any(color => color != BlockColor.None))
                    {
                        return rowIndex;
                    }
                }

                return -1;
            }

            private void ApplyColumnGravity()
            {
                int maxColumnCount = rows.Count > 0 ? rows.Max(row => row.Count) : 0;
                for (int colIndex = 0; colIndex < maxColumnCount; colIndex++)
                {
                    bool moved;
                    do
                    {
                        moved = false;
                        for (int rowIndex = rows.Count - 1; rowIndex >= 0; rowIndex--)
                        {
                            if (!TryGetCell(rowIndex, colIndex, out BlockColor targetColor) || targetColor != BlockColor.None)
                            {
                                continue;
                            }

                            int sourceRowIndex = FindNearestNonEmptyRowAbove(rowIndex, colIndex);
                            if (sourceRowIndex < 0)
                            {
                                continue;
                            }

                            rows[rowIndex][colIndex] = rows[sourceRowIndex][colIndex];
                            rows[sourceRowIndex][colIndex] = BlockColor.None;
                            moved = true;
                        }
                    } while (moved);
                }
            }

            private bool TryGetCell(int rowIndex, int colIndex, out BlockColor color)
            {
                color = BlockColor.None;
                if (rowIndex < 0 || rowIndex >= rows.Count)
                {
                    return false;
                }

                if (colIndex < 0 || colIndex >= rows[rowIndex].Count)
                {
                    return false;
                }

                color = rows[rowIndex][colIndex];
                return true;
            }

            private int FindNearestNonEmptyRowAbove(int rowIndex, int colIndex)
            {
                for (int aboveRowIndex = rowIndex - 1; aboveRowIndex >= 0; aboveRowIndex--)
                {
                    if (!TryGetCell(aboveRowIndex, colIndex, out BlockColor color))
                    {
                        continue;
                    }

                    if (color != BlockColor.None)
                    {
                        return aboveRowIndex;
                    }
                }

                return -1;
            }

            private void CollapseIfNeeded()
            {
                for (int i = rows.Count - 1; i >= 0; i--)
                {
                    if (rows[i].All(color => color == BlockColor.None))
                    {
                        rows.RemoveAt(i);
                    }
                }
            }
        }
    }
}
