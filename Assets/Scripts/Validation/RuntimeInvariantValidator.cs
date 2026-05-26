using System.Collections.Generic;
using System.Linq;
using System.Text;
using EliminateGame.Pattern;
using EliminateGame.TempZone;
using UnityEngine;

namespace EliminateGame.Validation
{
    public static class RuntimeInvariantValidator
    {
        public static bool Validate(
            Dictionary<BlockColor, int> patternCounts,
            Dictionary<BlockColor, int> selectionCounts,
            IReadOnlyList<TempZoneSlot> tempSlots,
            string context)
        {
            Dictionary<BlockColor, int> safePatternCounts = patternCounts ?? new Dictionary<BlockColor, int>();
            Dictionary<BlockColor, int> safeSelectionCounts = selectionCounts ?? new Dictionary<BlockColor, int>();
            IReadOnlyList<TempZoneSlot> safeTempSlots = tempSlots ?? System.Array.Empty<TempZoneSlot>();

            HashSet<BlockColor> colors = CollectColors(safePatternCounts, safeSelectionCounts, safeTempSlots);
            foreach (BlockColor color in colors.OrderBy(c => (int)c))
            {
                int patternRemaining = safePatternCounts.GetValueOrDefault(color, 0);
                int selectionRemaining = safeSelectionCounts.GetValueOrDefault(color, 0);
                int tempDebt = CalculateTempDebt(color, safeTempSlots);
                int expectedPattern = (selectionRemaining * 3) + tempDebt;

                if (patternRemaining == expectedPattern)
                {
                    continue;
                }

                Debug.LogError(BuildMismatchReport(
                    context,
                    color,
                    patternRemaining,
                    selectionRemaining,
                    tempDebt,
                    expectedPattern));

                return false;
            }

            return true;
        }

        private static HashSet<BlockColor> CollectColors(
            Dictionary<BlockColor, int> patternCounts,
            Dictionary<BlockColor, int> selectionCounts,
            IReadOnlyList<TempZoneSlot> tempSlots)
        {
            HashSet<BlockColor> colors = new HashSet<BlockColor>();

            foreach (BlockColor color in patternCounts.Keys)
            {
                if (color != BlockColor.None)
                {
                    colors.Add(color);
                }
            }

            foreach (BlockColor color in selectionCounts.Keys)
            {
                if (color != BlockColor.None)
                {
                    colors.Add(color);
                }
            }

            for (int i = 0; i < tempSlots.Count; i++)
            {
                TempZoneSlot slot = tempSlots[i];
                if (slot == null || slot.Color == BlockColor.None)
                {
                    continue;
                }

                colors.Add(slot.Color);
            }

            return colors;
        }

        private static int CalculateTempDebt(BlockColor color, IReadOnlyList<TempZoneSlot> tempSlots)
        {
            int tempDebt = 0;

            for (int i = 0; i < tempSlots.Count; i++)
            {
                TempZoneSlot slot = tempSlots[i];
                if (slot == null || slot.Color != color)
                {
                    continue;
                }

                tempDebt += 3 - slot.ProgressMark;
            }

            return tempDebt;
        }

        private static string BuildMismatchReport(
            string context,
            BlockColor color,
            int pattern,
            int selection,
            int tempDebt,
            int expected)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("[RUNTIME_INVARIANT]");
            builder.AppendLine($"Context={context}");
            builder.AppendLine($"Color={color}");
            builder.AppendLine($"Pattern={pattern}");
            builder.AppendLine($"Selection={selection}");
            builder.AppendLine($"TempDebt={tempDebt}");
            builder.Append($"Expected={expected}");
            return builder.ToString();
        }
    }
}
