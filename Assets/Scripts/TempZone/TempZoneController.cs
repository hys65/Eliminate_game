using System;
using System.Collections.Generic;
using System.Linq;
using EliminateGame.Pattern;
using UnityEngine;

namespace EliminateGame.TempZone
{
    public class TempZoneController : MonoBehaviour
    {
        [SerializeField, Min(1)] private int capacity = 7;

        private readonly List<TempZoneSlot> slots = new List<TempZoneSlot>();
        private readonly Dictionary<BlockColor, int> caseAProgressByColor = new Dictionary<BlockColor, int>();

        public int Capacity => capacity;
        public int Count => slots.Count;
        public bool IsFull => slots.Count >= capacity;
        public IReadOnlyList<TempZoneSlot> Slots => slots;

        public void Initialize(int newCapacity)
        {
            capacity = Mathf.Max(1, newCapacity);
            slots.Clear();
            caseAProgressByColor.Clear();
            Debug.Log($"Temp Zone initialized. Capacity={capacity}");
        }

        public int AddTile(BlockColor color)
        {
            if (IsFull)
            {
                Debug.LogWarning("Temp Zone is full. Cannot add tile.");
                return -1;
            }

            slots.Add(new TempZoneSlot(color));
            int index = slots.Count - 1;
            Debug.Log($"Temp Zone add: {color} at slot {index}. Count={slots.Count}/{capacity}");
            return index;
        }

        public bool ContainsColor(BlockColor color)
        {
            return slots.Any(slot => slot.Color == color);
        }

        public int RemoveByColor(BlockColor color, int removeCount)
        {
            int removed = 0;
            for (int i = slots.Count - 1; i >= 0 && removed < removeCount; i--)
            {
                if (slots[i].Color != color)
                {
                    continue;
                }

                slots.RemoveAt(i);
                removed++;
            }

            if (removed > 0)
            {
                Debug.Log($"Temp Zone removed {removed} tile(s) of color {color}. Count={slots.Count}/{capacity}");
            }

            return removed;
        }

        public void MarkCaseAProgress(BlockColor color, int targetSlotIndex, int gainedProgress)
        {
            int previous = caseAProgressByColor.GetValueOrDefault(color, 0);
            int next = Mathf.Clamp(previous + gainedProgress, 0, 2);
            caseAProgressByColor[color] = next;

            if (targetSlotIndex >= 0 && targetSlotIndex < slots.Count)
            {
                slots[targetSlotIndex].ProgressMark = next;
                Debug.Log($"Temp Zone progress mark on slot {targetSlotIndex} ({color}) set to {next}/3.");
            }
        }

        public bool HasAnyColorInSet(IReadOnlyCollection<BlockColor> colorSet)
        {
            foreach (TempZoneSlot slot in slots)
            {
                if (colorSet.Contains(slot.Color))
                {
                    return true;
                }
            }

            return false;
        }

        public List<TempZoneSlot> RemoveRescueTilesWeighted(IReadOnlyList<BlockColor> bottomRowColors, int amount, System.Random rng)
        {
            var removed = new List<TempZoneSlot>();
            if (slots.Count == 0 || amount <= 0)
            {
                return removed;
            }

            Dictionary<BlockColor, int> bottomCounts = new Dictionary<BlockColor, int>();
            foreach (BlockColor color in bottomRowColors)
            {
                bottomCounts[color] = bottomCounts.GetValueOrDefault(color, 0) + 1;
            }

            int toRemove = Mathf.Min(amount, slots.Count);
            for (int n = 0; n < toRemove; n++)
            {
                int chosenIndex = ChooseWeightedRemovalIndex(bottomCounts, rng);
                TempZoneSlot slot = slots[chosenIndex];
                removed.Add(slot);
                slots.RemoveAt(chosenIndex);
            }

            Debug.Log($"Temp Zone rescue removed {removed.Count} tile(s). Remaining={slots.Count}/{capacity}");
            return removed;
        }

        private int ChooseWeightedRemovalIndex(IReadOnlyDictionary<BlockColor, int> bottomCounts, System.Random rng)
        {
            float totalWeight = 0f;
            float[] weights = new float[slots.Count];

            for (int i = 0; i < slots.Count; i++)
            {
                TempZoneSlot slot = slots[i];
                int usefulness = bottomCounts.GetValueOrDefault(slot.Color, 0);

                // Lower usefulness for current Pattern bottom row => higher removal weight.
                float weight = usefulness > 0 ? 0.25f : 1.0f;
                weights[i] = weight;
                totalWeight += weight;
            }

            if (totalWeight <= 0f)
            {
                return rng.Next(0, slots.Count);
            }

            double roll = rng.NextDouble() * totalWeight;
            double cursor = 0d;
            for (int i = 0; i < slots.Count; i++)
            {
                cursor += weights[i];
                if (roll <= cursor)
                {
                    return i;
                }
            }

            return slots.Count - 1;
        }
    }
}
