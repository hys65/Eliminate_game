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
        [SerializeField] private Transform tileRoot;
        [SerializeField] private GameObject tileVisualPrefab;
        [SerializeField] private float spacing = 1.0f;

        private readonly List<TempZoneSlot> slots = new List<TempZoneSlot>();
        private readonly Dictionary<BlockColor, int> caseAProgressByColor = new Dictionary<BlockColor, int>();
        private readonly List<TileVisualEntry> tileVisuals = new List<TileVisualEntry>();

        private static Sprite cachedSolidSquareSprite;

        private sealed class TileVisualEntry
        {
            public BlockColor Color;
            public SpriteRenderer Renderer;
        }

        public int Capacity => capacity;
        public int Count => slots.Count;
        public bool IsFull => slots.Count >= capacity;
        public IReadOnlyList<TempZoneSlot> Slots => slots;

        public void Initialize(int newCapacity)
        {
            capacity = Mathf.Max(1, newCapacity);
            slots.Clear();
            caseAProgressByColor.Clear();
            ClearAllVisuals();
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
            CreateVisualForColor(color);
            RefreshVisualPositions();
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
                RemoveVisualAt(i);
                removed++;
            }

            if (removed > 0)
            {
                RefreshVisualPositions();
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
                ApplyProgressVisual(targetSlotIndex, next);
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
                RemoveVisualAt(chosenIndex);
            }

            RefreshVisualPositions();
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

        private void CreateVisualForColor(BlockColor color)
        {
            Transform root = GetTileRoot();
            if (root == null)
            {
                return;
            }

            GameObject visualObject = null;
            if (tileVisualPrefab != null)
            {
                visualObject = Instantiate(tileVisualPrefab, root);
            }

            if (visualObject == null)
            {
                visualObject = new GameObject("TempZoneTileVisual");
                visualObject.transform.SetParent(root, false);
            }

            SpriteRenderer visual = visualObject.GetComponent<SpriteRenderer>();
            if (visual == null)
            {
                visual = visualObject.AddComponent<SpriteRenderer>();
            }

            ForceSolidSquareStyle(visual, color);

            tileVisuals.Add(new TileVisualEntry
            {
                Color = color,
                Renderer = visual
            });
        }

        private void ForceSolidSquareStyle(SpriteRenderer renderer, BlockColor color)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.sprite = GetSolidSquareSprite();
            renderer.drawMode = SpriteDrawMode.Simple;
            renderer.color = MapColor(color);
            renderer.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
        }

        private static Sprite GetSolidSquareSprite()
        {
            if (cachedSolidSquareSprite != null)
            {
                return cachedSolidSquareSprite;
            }

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "TempZoneGeneratedTexture"
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            cachedSolidSquareSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            cachedSolidSquareSprite.name = "TempZoneGeneratedSprite";
            return cachedSolidSquareSprite;
        }

        private void RemoveVisualAt(int index)
        {
            if (index < 0 || index >= tileVisuals.Count)
            {
                return;
            }

            TileVisualEntry entry = tileVisuals[index];
            tileVisuals.RemoveAt(index);
            if (entry != null && entry.Renderer != null)
            {
                Destroy(entry.Renderer.gameObject);
            }
        }

        private void RefreshVisualPositions()
        {
            for (int i = tileVisuals.Count - 1; i >= 0; i--)
            {
                if (tileVisuals[i] == null || tileVisuals[i].Renderer == null)
                {
                    tileVisuals.RemoveAt(i);
                }
            }

            float startOffset = (tileVisuals.Count - 1) * spacing * 0.5f;

            for (int i = 0; i < tileVisuals.Count; i++)
            {
                TileVisualEntry entry = tileVisuals[i];
                SpriteRenderer renderer = entry.Renderer;
                if (renderer == null)
                {
                    continue;
                }

                renderer.transform.localPosition = new Vector3((i * spacing) - startOffset, 0f, 0f);
                renderer.transform.localScale = new Vector3(0.95f, 0.95f, 1f);

                int progress = i < slots.Count ? slots[i].ProgressMark : 0;
                renderer.color = ApplyProgressShade(MapColor(entry.Color), progress);
            }
        }

        private void ApplyProgressVisual(int index, int progressMark)
        {
            if (index < 0 || index >= tileVisuals.Count)
            {
                return;
            }

            TileVisualEntry entry = tileVisuals[index];
            if (entry == null || entry.Renderer == null)
            {
                return;
            }

            entry.Renderer.color = ApplyProgressShade(MapColor(entry.Color), progressMark);
        }

        private static Color ApplyProgressShade(Color baseColor, int progressMark)
        {
            switch (progressMark)
            {
                case 1:
                    return baseColor * 0.9f;
                case 2:
                    return baseColor * 0.8f;
                default:
                    return baseColor;
            }
        }

        private void ClearAllVisuals()
        {
            for (int i = 0; i < tileVisuals.Count; i++)
            {
                TileVisualEntry entry = tileVisuals[i];
                if (entry != null && entry.Renderer != null)
                {
                    Destroy(entry.Renderer.gameObject);
                }
            }

            tileVisuals.Clear();

            Transform root = GetTileRoot();
            if (root == null)
            {
                return;
            }

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Destroy(root.GetChild(i).gameObject);
            }
        }

        private Transform GetTileRoot()
        {
            return tileRoot != null ? tileRoot : transform;
        }

        private static Color MapColor(BlockColor color)
        {
            switch (color)
            {
                case BlockColor.Red:
                    return Color.red;
                case BlockColor.Blue:
                    return Color.blue;
                case BlockColor.Green:
                    return Color.green;
                case BlockColor.Yellow:
                    return Color.yellow;
                case BlockColor.Purple:
                    return new Color(0.6f, 0.2f, 0.8f);
                default:
                    return Color.white;
            }
        }
    }
}
