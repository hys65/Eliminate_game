using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EliminateGame.Pattern
{
    public class PatternController : MonoBehaviour
    {
        private readonly List<List<PatternCell>> patternRows = new List<List<PatternCell>>();

        public bool IsEmpty => patternRows.Count == 0;

        public void Initialize(IReadOnlyList<IReadOnlyList<BlockColor>> rows)
        {
            patternRows.Clear();

            foreach (IReadOnlyList<BlockColor> row in rows)
            {
                var builtRow = new List<PatternCell>();
                foreach (BlockColor color in row)
                {
                    if (color == BlockColor.None)
                    {
                        continue;
                    }

                    builtRow.Add(new PatternCell(color));
                }

                if (builtRow.Count > 0)
                {
                    patternRows.Add(builtRow);
                }
            }

            Debug.Log($"Pattern initialized. Rows={patternRows.Count}, Bottom=[{string.Join(",", GetBottomRowColors())}]");
        }

        public IReadOnlyList<BlockColor> GetBottomRowColors()
        {
            if (patternRows.Count == 0)
            {
                return Array.Empty<BlockColor>();
            }

            int bottomIndex = patternRows.Count - 1;
            return patternRows[bottomIndex].Select(cell => cell.Color).ToList();
        }

        public int GetBottomRowCount(BlockColor color)
        {
            if (patternRows.Count == 0)
            {
                return 0;
            }

            int bottomIndex = patternRows.Count - 1;
            return patternRows[bottomIndex].Count(c => c.Color == color);
        }

        public PatternResolveResult ResolveAgainstBottomRow(BlockColor color)
        {
            if (patternRows.Count == 0)
            {
                return PatternResolveResult.NoMatch();
            }

            int bottomIndex = patternRows.Count - 1;
            List<PatternCell> bottomRow = patternRows[bottomIndex];
            List<int> colorIndices = new List<int>();

            for (int i = 0; i < bottomRow.Count; i++)
            {
                if (bottomRow[i].Color == color)
                {
                    colorIndices.Add(i);
                }
            }

            if (colorIndices.Count == 0)
            {
                return PatternResolveResult.NoMatch();
            }

            if (colorIndices.Count < 3)
            {
                // Case A: remove all occurrences from Pattern bottom row (left-to-right deterministic).
                RemoveCellsAtIndices(bottomRow, colorIndices);
                CollapseIfNeeded();
                Debug.Log($"Pattern Case A resolved for {color}. Removed={colorIndices.Count} from bottom row.");
                return PatternResolveResult.CaseA(colorIndices.Count);
            }

            // Case B: remove exactly 3 occurrences, deterministic left-to-right.
            List<int> firstThree = colorIndices.Take(3).ToList();
            RemoveCellsAtIndices(bottomRow, firstThree);
            CollapseIfNeeded();
            Debug.Log($"Pattern Case B resolved for {color}. Removed=3 from bottom row (left-to-right).");
            return PatternResolveResult.CaseB(3);
        }

        private static void RemoveCellsAtIndices(List<PatternCell> row, List<int> indicesAscending)
        {
            for (int i = indicesAscending.Count - 1; i >= 0; i--)
            {
                int index = indicesAscending[i];
                row.RemoveAt(index);
            }
        }

        private void CollapseIfNeeded()
        {
            // Conceptual fall-down: remove empty rows so rows above become the new bottom.
            for (int i = patternRows.Count - 1; i >= 0; i--)
            {
                if (patternRows[i].Count == 0)
                {
                    patternRows.RemoveAt(i);
                }
            }

            Debug.Log($"Pattern collapse check complete. Rows={patternRows.Count}, Bottom=[{string.Join(",", GetBottomRowColors())}]");
        }
    }

    public readonly struct PatternResolveResult
    {
        public readonly bool Matched;
        public readonly bool IsCaseA;
        public readonly int PatternRemovedCount;

        private PatternResolveResult(bool matched, bool isCaseA, int patternRemovedCount)
        {
            Matched = matched;
            IsCaseA = isCaseA;
            PatternRemovedCount = patternRemovedCount;
        }

        public static PatternResolveResult NoMatch() => new PatternResolveResult(false, false, 0);

        public static PatternResolveResult CaseA(int removed) => new PatternResolveResult(true, true, removed);

        public static PatternResolveResult CaseB(int removed) => new PatternResolveResult(true, false, removed);
    }
}
