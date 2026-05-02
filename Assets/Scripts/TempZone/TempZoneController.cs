using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EliminateGame.Pattern;
using TMPro;
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
        private readonly List<TileVisualEntry> tileVisuals = new List<TileVisualEntry>();
        private PatternController cachedPatternController;

        private static Sprite cachedSolidSquareSprite;

        private sealed class TileVisualEntry
        {
            public BlockColor Color;
            public SpriteRenderer Renderer;
            public TextMeshPro ProgressText;
            public Coroutine FeedbackCoroutine;
        }

        public int Capacity => capacity;
        public int Count => slots.Count;
        public bool IsFull => slots.Count >= capacity;
        public IReadOnlyList<TempZoneSlot> Slots => slots;

        public void Initialize(int newCapacity)
        {
            capacity = Mathf.Max(1, newCapacity);
            slots.Clear();
            ClearAllVisuals();
            Debug.Log($"Temp Zone initialized. Capacity={capacity}");
        }

        public void Clear()
        {
            slots.Clear();
            ClearAllVisuals();
            Debug.Log("Temp Zone cleared.");
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

            if (DoesTileMatchCurrentBottomRow(color))
            {
                PlayAddMatchFeedbackOnLatestTile();
            }

            Debug.Log($"Temp Zone add: {color} at slot {index}. Count={slots.Count}/{capacity}");
            return index;
        }

        public bool ContainsColor(BlockColor color)
        {
            return slots.Any(slot => slot.Color == color);
        }

        public bool TryFindMatchingSlot(IReadOnlyCollection<BlockColor> bottomRowColors, out int slotIndex, out BlockColor color)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (!bottomRowColors.Contains(slots[i].Color))
                {
                    continue;
                }

                slotIndex = i;
                color = slots[i].Color;
                return true;
            }

            slotIndex = -1;
            color = BlockColor.None;
            return false;
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

        public void ApplyCaseAProgress(int targetSlotIndex, int gainedProgress)
        {
            if (targetSlotIndex < 0 || targetSlotIndex >= slots.Count || gainedProgress <= 0)
            {
                return;
            }

            TempZoneSlot slot = slots[targetSlotIndex];
            int previous = slot.ProgressMark;
            slot.IncreaseProgressMark(gainedProgress, 3);
            int next = slot.ProgressMark;

            Debug.Log($"Temp Zone progress mark on slot {targetSlotIndex} ({slot.Color}) set to {next}/3.");

            if (next >= 3)
            {
                BlockColor color = slot.Color;
                RemoveSlotAt(targetSlotIndex);
                Debug.Log($"Temp Zone removed slot {targetSlotIndex} ({color}) after reaching 3/3 progress (from {previous}/3 +{gainedProgress}).");
                return;
            }

            ApplyProgressVisual(targetSlotIndex, next);
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

        public void RemoveSlotAtIndex(int index)
        {
            RemoveSlotAt(index);
        }

        private void RemoveSlotAt(int index)
        {
            if (index < 0 || index >= slots.Count)
            {
                return;
            }

            slots.RemoveAt(index);
            RemoveVisualAt(index);
            RefreshVisualPositions();
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
                Renderer = visual,
                ProgressText = GetOrCreateProgressText(visualObject)
            });

            int progress = slots.Count > 0 ? slots[slots.Count - 1].ProgressMark : 0;
            UpdateProgressText(tileVisuals[tileVisuals.Count - 1], progress);
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
                UpdateProgressText(entry, progress);
            }
        }

        private bool DoesTileMatchCurrentBottomRow(BlockColor color)
        {
            PatternController patternController = GetPatternController();
            if (patternController == null)
            {
                return false;
            }

            IReadOnlyList<BlockColor> bottomRowColors = patternController.GetBottomRowColors();
            return bottomRowColors.Contains(color);
        }

        private PatternController GetPatternController()
        {
            if (cachedPatternController == null)
            {
                cachedPatternController = FindObjectOfType<PatternController>();
            }

            return cachedPatternController;
        }

        private void PlayAddMatchFeedbackOnLatestTile()
        {
            if (tileVisuals.Count == 0)
            {
                return;
            }

            TileVisualEntry entry = tileVisuals[tileVisuals.Count - 1];
            if (entry == null || entry.Renderer == null)
            {
                return;
            }

            if (entry.FeedbackCoroutine != null)
            {
                StopCoroutine(entry.FeedbackCoroutine);
            }

            entry.FeedbackCoroutine = StartCoroutine(PlayHitFeedback(entry));
        }

        private IEnumerator PlayHitFeedback(TileVisualEntry entry)
        {
            if (entry == null || entry.Renderer == null)
            {
                yield break;
            }

            Transform tileTransform = entry.Renderer.transform;
            Vector3 normalScale = new Vector3(0.95f, 0.95f, 1f);
            Vector3 punchScale = normalScale * 1.2f;

            int index = tileVisuals.IndexOf(entry);
            int progress = index >= 0 && index < slots.Count ? slots[index].ProgressMark : 0;
            Color baseColor = ApplyProgressShade(MapColor(entry.Color), progress);
            Color brightColor = baseColor * 1.15f;
            brightColor.a = baseColor.a;

            const float totalDuration = 0.1f;
            float halfDuration = totalDuration * 0.5f;
            float elapsed = 0f;

            while (elapsed < halfDuration)
            {
                if (entry.Renderer == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                tileTransform.localScale = Vector3.LerpUnclamped(normalScale, punchScale, t);
                entry.Renderer.color = Color.Lerp(baseColor, brightColor, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                if (entry.Renderer == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                tileTransform.localScale = Vector3.LerpUnclamped(punchScale, normalScale, t);
                entry.Renderer.color = Color.Lerp(brightColor, baseColor, t);
                yield return null;
            }

            if (entry.Renderer != null)
            {
                tileTransform.localScale = normalScale;
                entry.Renderer.color = baseColor;
            }

            entry.FeedbackCoroutine = null;
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
            UpdateProgressText(entry, progressMark);
        }

        private static TextMeshPro GetOrCreateProgressText(GameObject visualObject)
        {
            if (visualObject == null)
            {
                return null;
            }

            TextMeshPro text = visualObject.GetComponentInChildren<TextMeshPro>();
            if (text != null)
            {
                return text;
            }

            var textObject = new GameObject("ProgressText");
            textObject.transform.SetParent(visualObject.transform, false);
            textObject.transform.localPosition = new Vector3(0f, 0f, -0.1f);

            text = textObject.AddComponent<TextMeshPro>();
            text.fontSize = 2.6f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.black;
            text.outlineWidth = 0.15f;
            text.outlineColor = Color.white;
            text.text = "0/3";
            text.sortingOrder = 10;
            return text;
        }

        private static void UpdateProgressText(TileVisualEntry entry, int progressMark)
        {
            if (entry == null || entry.ProgressText == null)
            {
                return;
            }

            int clamped = Mathf.Clamp(progressMark, 0, 3);
            entry.ProgressText.text = $"{clamped}/3";
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
