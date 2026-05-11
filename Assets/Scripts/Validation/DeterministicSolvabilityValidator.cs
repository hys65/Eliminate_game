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
        private const int AutoResolveSafetyLimit = 256;

        public static bool Validate(GameConfig config, string context)
        {
            string safeContext = string.IsNullOrWhiteSpace(context) ? "UnknownContext" : context;

            if (config == null)
            {
                Debug.LogError($"[SOLVABILITY_VALIDATION][{safeContext}] FAILED: GameConfig is null.");
                return false;
            }

            Snapshot snapshot = Snapshot.FromConfig(config);
            List<string> failures = new List<string>();

            ValidateColorCountRule(snapshot, failures);
            ValidateStartUnlockReachability(snapshot, failures);
            ValidatePlayableSequence(snapshot, failures);

            if (failures.Count > 0)
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine($"[SOLVABILITY_VALIDATION][{safeContext}] FAILED");
                for (int i = 0; i < failures.Count; i++)
                {
                    builder.AppendLine($"- {failures[i]}");
                }

                Debug.LogError(builder.ToString());
                return false;
            }

            Debug.Log($"[SOLVABILITY_VALIDATION][{safeContext}] PASSED: deterministic solvability validation succeeded.");
            return true;
        }

        private static void ValidateColorCountRule(Snapshot snapshot, List<string> failures)
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
                int expected = selectionCount * 3;

                if (patternCount != expected)
                {
                    failures.Add($"Color count rule violation for {color}: PatternCount={patternCount}, SelectionCount={selectionCount}, expected PatternCount=SelectionCount*3={expected}.");
                }
            }
        }

        private static void ValidateStartUnlockReachability(Snapshot snapshot, List<string> failures)
        {
            List<SelectionNode> sortedNodes = snapshot.Nodes.Values
                .OrderBy(n => n.Position.y)
                .ThenBy(n => n.Position.x)
                .ToList();

            List<SelectionNode> startNodes = sortedNodes
                .Where(n => n.StartUnlocked && n.Color != BlockColor.None)
                .ToList();

            if (startNodes.Count == 0)
            {
                failures.Add("SelectionArea has no StartUnlocked non-None tile.");
                return;
            }

            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

            foreach (SelectionNode start in startNodes)
            {
                visited.Add(start.Position);
                queue.Enqueue(start.Position);
            }

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                foreach (Vector2Int neighbor in GetOrthogonalNeighborsOrdered(current))
                {
                    if (!snapshot.Nodes.ContainsKey(neighbor))
                    {
                        continue;
                    }

                    if (visited.Add(neighbor))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            foreach (SelectionNode node in sortedNodes)
            {
                if (!visited.Contains(node.Position))
                {
                    failures.Add($"Unreachable SelectionArea tile from StartUnlocked seeds: position=({node.Position.x},{node.Position.y}), color={node.Color}.");
                }
            }
        }

        private static void ValidatePlayableSequence(Snapshot snapshot, List<string> failures)
        {
            SearchState initial = SearchState.FromSnapshot(snapshot);
            Queue<SearchState> frontier = new Queue<SearchState>();
            HashSet<string> visited = new HashSet<string>();

            frontier.Enqueue(initial);
            visited.Add(initial.BuildKey());

            int explored = 0;
            string bestFailure = "No deterministic winning sequence found.";

            while (frontier.Count > 0)
            {
                SearchState state = frontier.Dequeue();
                explored++;

                if (state.Pattern.IsEmpty())
                {
                    return;
                }

                if (explored > MaxSearchNodes)
                {
                    failures.Add($"Search safety limit reached: exploredNodes={explored}, MaxSearchNodes={MaxSearchNodes}.");
                    return;
                }

                if (state.IsLoseState())
                {
                    bestFailure = "Encountered LOSE state (TempZone full and no color matches current Pattern bottom row).";
                    continue;
                }

                List<SelectionNode> candidates = state.GetDeterministicUnlockedRemainingTiles();
                if (candidates.Count == 0)
                {
                    bestFailure = "No unlocked SelectionArea choices remain before Pattern is empty.";
                    continue;
                }

                bool expanded = false;
                foreach (SelectionNode candidate in candidates)
                {
                    SearchState next = state.Clone();
                    SimulateSelection(next, candidate);

                    if (next.IsLoseState())
                    {
                        bestFailure = $"LOSE after selecting ({candidate.Position.x},{candidate.Position.y}) {candidate.Color}: TempZone full with no bottom-row color match.";
                        continue;
                    }

                    string key = next.BuildKey();
                    if (!visited.Add(key))
                    {
                        continue;
                    }

                    expanded = true;
                    frontier.Enqueue(next);
                }

                if (!expanded)
                {
                    bestFailure = "All reachable deterministic branches either repeat previous states or end in LOSE.";
                }
            }

            failures.Add($"Playable sequence solvability failed. {bestFailure}");
        }

        private static void SimulateSelection(SearchState state, SelectionNode candidate)
        {
            state.RemovedSelectionPositions.Add(candidate.Position);
            state.TempSlots.Add(new TempSlot(candidate.Color, 0));
            state.UnlockOrthogonalNeighbors(candidate.Position);

            int selectedSlotIndex = state.FindFirstTempSlotIndexByColor(candidate.Color);
            if (selectedSlotIndex >= 0)
            {
                ResolveAgainstTempSlot(state, candidate.Color, selectedSlotIndex);
            }

            SimulateAutoResolveChain(state);
            CleanupStaleTempSlots(state);
        }

        private static void SimulateAutoResolveChain(SearchState state)
        {
            for (int step = 0; step < AutoResolveSafetyLimit; step++)
            {
                if (state.Pattern.IsEmpty())
                {
                    return;
                }

                HashSet<BlockColor> bottomColors = new HashSet<BlockColor>(state.Pattern.GetBottomRowColors());
                if (bottomColors.Count == 0)
                {
                    return;
                }

                int matchingSlotIndex = -1;
                BlockColor matchingColor = BlockColor.None;
                for (int i = 0; i < state.TempSlots.Count; i++)
                {
                    BlockColor slotColor = state.TempSlots[i].Color;
                    if (!bottomColors.Contains(slotColor))
                    {
                        continue;
                    }

                    matchingSlotIndex = i;
                    matchingColor = slotColor;
                    break;
                }

                if (matchingSlotIndex < 0)
                {
                    return;
                }

                bool changed = ResolveAgainstTempSlot(state, matchingColor, matchingSlotIndex);
                if (!changed)
                {
                    return;
                }
            }
        }

        private static bool ResolveAgainstTempSlot(SearchState state, BlockColor color, int targetSlotIndex)
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

            bool canCaseB = bottomRowCount >= 3 && sameColorTempCount >= 3;
            if (canCaseB)
            {
                int removedTemp = 0;
                for (int i = state.TempSlots.Count - 1; i >= 0 && removedTemp < 3; i--)
                {
                    if (state.TempSlots[i].Color != color)
                    {
                        continue;
                    }

                    state.TempSlots.RemoveAt(i);
                    removedTemp++;
                }
            }
            else
            {
                if (targetSlotIndex < 0 || targetSlotIndex >= state.TempSlots.Count)
                {
                    return true;
                }

                TempSlot slot = state.TempSlots[targetSlotIndex];
                slot.Progress = Mathf.Clamp(slot.Progress + removedFromPattern, 0, 3);
                if (slot.Progress >= 3)
                {
                    state.TempSlots.RemoveAt(targetSlotIndex);
                }
                else
                {
                    state.TempSlots[targetSlotIndex] = slot;
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

        private static IEnumerable<Vector2Int> GetOrthogonalNeighborsOrdered(Vector2Int position)
        {
            yield return new Vector2Int(position.x, position.y - 1);
            yield return new Vector2Int(position.x - 1, position.y);
            yield return new Vector2Int(position.x + 1, position.y);
            yield return new Vector2Int(position.x, position.y + 1);
        }

        private readonly struct TempSlot
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

        private sealed class Snapshot
        {
            public readonly Dictionary<Vector2Int, SelectionNode> Nodes = new Dictionary<Vector2Int, SelectionNode>();
            public readonly Dictionary<BlockColor, int> PatternCounts = new Dictionary<BlockColor, int>();
            public readonly Dictionary<BlockColor, int> SelectionCounts = new Dictionary<BlockColor, int>();
            public readonly List<List<BlockColor>> PatternRows = new List<List<BlockColor>>();
            public int TempZoneCapacity;

            public static Snapshot FromConfig(GameConfig config)
            {
                Snapshot snapshot = new Snapshot
                {
                    TempZoneCapacity = Mathf.Max(1, config.TempZoneCapacity)
                };

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

                foreach (GameConfig.SelectionTileDefinition tile in config.SelectionTiles)
                {
                    if (tile.Color == BlockColor.None)
                    {
                        continue;
                    }

                    Vector2Int position = new Vector2Int(tile.X, tile.Y);
                    if (snapshot.Nodes.ContainsKey(position))
                    {
                        continue;
                    }

                    snapshot.SelectionCounts[tile.Color] = snapshot.SelectionCounts.GetValueOrDefault(tile.Color, 0) + 1;
                    snapshot.Nodes[position] = new SelectionNode
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
            public readonly Dictionary<Vector2Int, SelectionNode> Nodes = new Dictionary<Vector2Int, SelectionNode>();
            public readonly HashSet<Vector2Int> RemovedSelectionPositions = new HashSet<Vector2Int>();
            public readonly HashSet<Vector2Int> UnlockedPositions = new HashSet<Vector2Int>();
            public readonly List<TempSlot> TempSlots = new List<TempSlot>();
            public int TempZoneCapacity;

            public static SearchState FromSnapshot(Snapshot snapshot)
            {
                SearchState state = new SearchState
                {
                    Pattern = new PatternState(snapshot.PatternRows),
                    TempZoneCapacity = snapshot.TempZoneCapacity
                };

                foreach (KeyValuePair<Vector2Int, SelectionNode> entry in snapshot.Nodes)
                {
                    state.Nodes[entry.Key] = entry.Value;
                    if (entry.Value.StartUnlocked)
                    {
                        state.UnlockedPositions.Add(entry.Key);
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

                foreach (KeyValuePair<Vector2Int, SelectionNode> entry in Nodes)
                {
                    clone.Nodes[entry.Key] = entry.Value;
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
                return clone;
            }

            public List<SelectionNode> GetDeterministicUnlockedRemainingTiles()
            {
                List<SelectionNode> result = new List<SelectionNode>();
                foreach (Vector2Int pos in UnlockedPositions.OrderBy(p => p.y).ThenBy(p => p.x))
                {
                    if (RemovedSelectionPositions.Contains(pos))
                    {
                        continue;
                    }

                    if (Nodes.TryGetValue(pos, out SelectionNode node))
                    {
                        result.Add(node);
                    }
                }

                return result;
            }

            public void UnlockOrthogonalNeighbors(Vector2Int from)
            {
                foreach (Vector2Int neighbor in GetOrthogonalNeighborsOrdered(from))
                {
                    if (Nodes.ContainsKey(neighbor))
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

            public PatternState Clone() => new PatternState(rows);

            public bool IsEmpty() => GetBottomRowIndex() < 0;

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
                int bottom = GetBottomRowIndex();
                return bottom < 0
                    ? new List<BlockColor>()
                    : rows[bottom].Where(c => c != BlockColor.None).ToList();
            }

            public int GetBottomRowCount(BlockColor color)
            {
                int bottom = GetBottomRowIndex();
                return bottom < 0 ? 0 : rows[bottom].Count(c => c == color);
            }

            public int ResolveAgainstBottomRow(BlockColor color)
            {
                int bottom = GetBottomRowIndex();
                if (bottom < 0)
                {
                    return 0;
                }

                List<BlockColor> row = rows[bottom];
                List<int> indices = new List<int>();
                for (int i = 0; i < row.Count; i++)
                {
                    if (row[i] == color)
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
                    row[indices[i]] = BlockColor.None;
                }

                ApplyColumnGravityOnly();
                CollapseEmptyRows();
                return removeCount;
            }

            public string BuildKey()
            {
                return string.Join("/", rows.Select(row => string.Join(",", row.Select(c => ((int)c).ToString()))));
            }

            private int GetBottomRowIndex()
            {
                for (int r = rows.Count - 1; r >= 0; r--)
                {
                    if (rows[r].Any(c => c != BlockColor.None))
                    {
                        return r;
                    }
                }

                return -1;
            }

            private void ApplyColumnGravityOnly()
            {
                int maxCols = rows.Count == 0 ? 0 : rows.Max(r => r.Count);
                for (int x = 0; x < maxCols; x++)
                {
                    bool moved;
                    do
                    {
                        moved = false;
                        for (int y = rows.Count - 1; y >= 0; y--)
                        {
                            if (!TryGetCell(y, x, out BlockColor current) || current != BlockColor.None)
                            {
                                continue;
                            }

                            int src = FindNearestNonEmptyAbove(y, x);
                            if (src < 0)
                            {
                                continue;
                            }

                            rows[y][x] = rows[src][x];
                            rows[src][x] = BlockColor.None;
                            moved = true;
                        }
                    } while (moved);
                }
            }

            private void CollapseEmptyRows()
            {
                for (int i = rows.Count - 1; i >= 0; i--)
                {
                    if (rows[i].All(c => c == BlockColor.None))
                    {
                        rows.RemoveAt(i);
                    }
                }
            }

            private bool TryGetCell(int y, int x, out BlockColor color)
            {
                color = BlockColor.None;
                if (y < 0 || y >= rows.Count)
                {
                    return false;
                }

                if (x < 0 || x >= rows[y].Count)
                {
                    return false;
                }

                color = rows[y][x];
                return true;
            }

            private int FindNearestNonEmptyAbove(int y, int x)
            {
                for (int r = y - 1; r >= 0; r--)
                {
                    if (!TryGetCell(r, x, out BlockColor color))
                    {
                        continue;
                    }

                    if (color != BlockColor.None)
                    {
                        return r;
                    }
                }

                return -1;
            }
        }
    }
}
