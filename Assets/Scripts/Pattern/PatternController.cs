using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EliminateGame.Pattern
{
    public class PatternController : MonoBehaviour
    {
        [Header("Pattern Visual")]
        [SerializeField] private Transform tileRoot;
        [SerializeField] private GameObject tileVisualPrefab;
        [SerializeField, Min(0.01f)] private float tileSpacing = 1f;
        [SerializeField, Min(0.01f)] private float visualScale = 0.95f;
        [SerializeField, Min(0f)] private float baseFallDuration = 0.08f;
        [SerializeField, Min(0f)] private float perCellFallTime = 0.06f;
        [SerializeField] private int sortingOrderBase = 300;
        [SerializeField, Min(1)] private int sortingOrderRowStride = 20;

        private readonly List<List<PatternCell>> patternRows = new List<List<PatternCell>>();
        private readonly List<TileVisualEntry> tileVisuals = new List<TileVisualEntry>();

        private Coroutine fallAnimationCoroutine;

        private static Sprite cachedSolidSquareSprite;

        private readonly struct TileStableKey : IEquatable<TileStableKey>
        {
            public readonly int Column;
            public readonly BlockColor Color;
            public readonly int Occurrence;

            public TileStableKey(int column, BlockColor color, int occurrence)
            {
                Column = column;
                Color = color;
                Occurrence = occurrence;
            }

            public bool Equals(TileStableKey other)
            {
                return Column == other.Column && Color == other.Color && Occurrence == other.Occurrence;
            }

            public override bool Equals(object obj)
            {
                return obj is TileStableKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = Column;
                    hash = (hash * 397) ^ (int)Color;
                    hash = (hash * 397) ^ Occurrence;
                    return hash;
                }
            }
        }

        private sealed class TileVisualEntry
        {
            public SpriteRenderer Renderer;
            public Vector3 TargetLocalPosition;
            public int SortingOrder;
        }

        public bool IsEmpty => GetBottomRowIndex() < 0;

        public void Initialize(IReadOnlyList<IReadOnlyList<BlockColor>> rows)
        {
            patternRows.Clear();

            foreach (IReadOnlyList<BlockColor> row in rows)
            {
                var builtRow = new List<PatternCell>();

                foreach (BlockColor color in row)
                {
                    builtRow.Add(new PatternCell(color));
                }

                patternRows.Add(builtRow);
            }

            RefreshVisualsInstant();
            Debug.Log($"Pattern initialized. Rows={patternRows.Count}, Bottom=[{string.Join(",", GetBottomRowColors())}]");
        }

        public IReadOnlyList<BlockColor> GetBottomRowColors()
        {
            int bottomIndex = GetBottomRowIndex();
            if (bottomIndex < 0)
            {
                return Array.Empty<BlockColor>();
            }

            List<PatternCell> bottomRow = patternRows[bottomIndex];
            return bottomRow.Where(cell => cell.Color != BlockColor.None).Select(cell => cell.Color).ToList();
        }

        public int GetBottomRowCount(BlockColor color)
        {
            int bottomIndex = GetBottomRowIndex();
            if (bottomIndex < 0)
            {
                return 0;
            }

            return patternRows[bottomIndex].Count(c => c.Color == color);
        }

        public PatternResolveResult ResolveAgainstBottomRow(BlockColor color)
        {
            int bottomIndex = GetBottomRowIndex();
            if (bottomIndex < 0)
            {
                return PatternResolveResult.NoMatch();
            }

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
                SetCellsToNone(bottomRow, colorIndices);
                ApplyColumnGravity();
                CollapseIfNeeded();
                RefreshVisualsWithFallAnimation();
                Debug.Log($"Pattern Case A resolved for {color}. Removed={colorIndices.Count} from bottom row.");
                return PatternResolveResult.CaseA(colorIndices.Count);
            }

            List<int> firstThree = colorIndices.Take(3).ToList();
            SetCellsToNone(bottomRow, firstThree);
            ApplyColumnGravity();
            CollapseIfNeeded();
            RefreshVisualsWithFallAnimation();
            Debug.Log($"Pattern Case B resolved for {color}. Removed=3 from bottom row (left-to-right).");
            return PatternResolveResult.CaseB(3);
        }

        private int GetBottomRowIndex()
        {
            for (int rowIndex = patternRows.Count - 1; rowIndex >= 0; rowIndex--)
            {
                List<PatternCell> row = patternRows[rowIndex];
                for (int colIndex = 0; colIndex < row.Count; colIndex++)
                {
                    if (row[colIndex].Color != BlockColor.None)
                    {
                        return rowIndex;
                    }
                }
            }

            return -1;
        }

        private static void SetCellsToNone(List<PatternCell> row, List<int> indices)
        {
            for (int i = 0; i < indices.Count; i++)
            {
                int index = indices[i];
                if (index >= 0 && index < row.Count)
                {
                    row[index].Color = BlockColor.None;
                }
            }
        }

        private void ApplyColumnGravity()
        {
            int maxColumnCount = 0;
            for (int rowIndex = 0; rowIndex < patternRows.Count; rowIndex++)
            {
                if (patternRows[rowIndex].Count > maxColumnCount)
                {
                    maxColumnCount = patternRows[rowIndex].Count;
                }
            }

            for (int colIndex = 0; colIndex < maxColumnCount; colIndex++)
            {
                bool moved;
                do
                {
                    moved = false;

                    for (int rowIndex = patternRows.Count - 1; rowIndex >= 0; rowIndex--)
                    {
                        if (!TryGetCell(rowIndex, colIndex, out PatternCell targetCell))
                        {
                            continue;
                        }

                        if (targetCell.Color != BlockColor.None)
                        {
                            continue;
                        }

                        int sourceRowIndex = FindNearestNonEmptyRowAbove(rowIndex, colIndex);
                        if (sourceRowIndex < 0)
                        {
                            continue;
                        }

                        PatternCell sourceCell = patternRows[sourceRowIndex][colIndex];
                        targetCell.Color = sourceCell.Color;
                        sourceCell.Color = BlockColor.None;
                        moved = true;
                    }
                } while (moved);
            }
        }

        private bool TryGetCell(int rowIndex, int colIndex, out PatternCell cell)
        {
            cell = null;
            if (rowIndex < 0 || rowIndex >= patternRows.Count)
            {
                return false;
            }

            List<PatternCell> row = patternRows[rowIndex];
            if (colIndex < 0 || colIndex >= row.Count)
            {
                return false;
            }

            cell = row[colIndex];
            return true;
        }

        private int FindNearestNonEmptyRowAbove(int rowIndex, int colIndex)
        {
            for (int aboveRowIndex = rowIndex - 1; aboveRowIndex >= 0; aboveRowIndex--)
            {
                if (!TryGetCell(aboveRowIndex, colIndex, out PatternCell aboveCell))
                {
                    continue;
                }

                if (aboveCell.Color != BlockColor.None)
                {
                    return aboveRowIndex;
                }
            }

            return -1;
        }

        private void CollapseIfNeeded()
        {
            for (int i = patternRows.Count - 1; i >= 0; i--)
            {
                if (IsAllNoneRow(patternRows[i]))
                {
                    patternRows.RemoveAt(i);
                }
            }

            Debug.Log($"Pattern collapse check complete. Rows={patternRows.Count}, Bottom=[{string.Join(",", GetBottomRowColors())}]");
        }

        private static bool IsAllNoneRow(List<PatternCell> row)
        {
            for (int i = 0; i < row.Count; i++)
            {
                if (row[i].Color != BlockColor.None)
                {
                    return false;
                }
            }

            return true;
        }

        private void RefreshVisualsInstant()
        {
            StopFallAnimationIfRunning();
            ClearAllVisuals();

            Transform root = GetTileRoot();
            if (root == null)
            {
                return;
            }

            BuildVisuals(root, null);
        }

        private void RefreshVisualsWithFallAnimation()
        {
            StopFallAnimationIfRunning();

            Transform root = GetTileRoot();
            if (root == null)
            {
                ClearAllVisuals();
                return;
            }

            Dictionary<TileStableKey, Queue<TileVisualEntry>> oldVisualsByKey = CaptureCurrentVisualsByKey();
            List<TileVisualEntry> unmatchedOldVisuals = new List<TileVisualEntry>(tileVisuals);

            tileVisuals.Clear();
            BuildVisuals(root, oldVisualsByKey);
            DestroyRemainingOldVisuals(oldVisualsByKey, unmatchedOldVisuals);

            if (baseFallDuration <= 0f && perCellFallTime <= 0f)
            {
                SnapVisualsToTargetPositions();
                return;
            }

            fallAnimationCoroutine = StartCoroutine(AnimateFallCoroutine());
        }

        private void BuildVisuals(Transform root, Dictionary<TileStableKey, Queue<TileVisualEntry>> oldVisualsByKey)
        {
            int maxColumnCount = 0;
            for (int rowIndex = 0; rowIndex < patternRows.Count; rowIndex++)
            {
                if (patternRows[rowIndex].Count > maxColumnCount)
                {
                    maxColumnCount = patternRows[rowIndex].Count;
                }
            }

            float globalCenterOffset = (maxColumnCount - 1) * tileSpacing * 0.5f;

            Dictionary<(int Column, BlockColor Color), int> occurrenceCounters = new Dictionary<(int Column, BlockColor Color), int>();

            for (int rowIndex = 0; rowIndex < patternRows.Count; rowIndex++)
            {
                List<PatternCell> row = patternRows[rowIndex];
                float y = (patternRows.Count - 1 - rowIndex) * tileSpacing;

                for (int colIndex = 0; colIndex < row.Count; colIndex++)
                {
                    BlockColor color = row[colIndex].Color;
                    if (color == BlockColor.None)
                    {
                        continue;
                    }

                    var occurrenceKey = (Column: colIndex, Color: color);
                    if (!occurrenceCounters.TryGetValue(occurrenceKey, out int occurrence))
                    {
                        occurrence = 0;
                    }

                    TileStableKey stableKey = new TileStableKey(colIndex, color, occurrence);
                    occurrenceCounters[occurrenceKey] = occurrence + 1;

                    float x = (colIndex * tileSpacing) - globalCenterOffset;
                    Vector3 targetLocalPosition = new Vector3(x, y, 0f);
                    int sortingOrder = sortingOrderBase + ((patternRows.Count - 1 - rowIndex) * sortingOrderRowStride) + colIndex;

                    TileVisualEntry entry = TryReuseOldVisual(stableKey, oldVisualsByKey);
                    if (entry == null || entry.Renderer == null)
                    {
                        SpriteRenderer renderer = CreateTileRenderer(root, color);
                        if (renderer == null)
                        {
                            continue;
                        }

                        Transform tileTransform = renderer.transform;
                        tileTransform.localPosition = targetLocalPosition;
                        tileTransform.localRotation = Quaternion.identity;
                        tileTransform.localScale = GetCompensatedVisualScale(tileTransform.parent);

                        entry = new TileVisualEntry
                        {
                            Renderer = renderer
                        };
                    }
                    else
                    {
                        Transform tileTransform = entry.Renderer.transform;
                        tileTransform.SetParent(root, false);
                        tileTransform.localRotation = Quaternion.identity;
                        tileTransform.localScale = GetCompensatedVisualScale(tileTransform.parent);
                        entry.Renderer.color = MapColor(color);
                    }

                    entry.SortingOrder = sortingOrder;
                    entry.TargetLocalPosition = targetLocalPosition;
                    entry.Renderer.sortingOrder = sortingOrder;
                    tileVisuals.Add(entry);
                }
            }
        }

        private TileVisualEntry TryReuseOldVisual(TileStableKey key, Dictionary<TileStableKey, Queue<TileVisualEntry>> oldVisualsByKey)
        {
            if (oldVisualsByKey == null)
            {
                return null;
            }

            if (!oldVisualsByKey.TryGetValue(key, out Queue<TileVisualEntry> queue) || queue.Count == 0)
            {
                return null;
            }

            TileVisualEntry entry = queue.Dequeue();
            if (entry == null || entry.Renderer == null)
            {
                return null;
            }

            return entry;
        }

        private Dictionary<TileStableKey, Queue<TileVisualEntry>> CaptureCurrentVisualsByKey()
        {
            var result = new Dictionary<TileStableKey, Queue<TileVisualEntry>>();
            var occurrenceCounters = new Dictionary<(int Column, BlockColor Color), int>();

            int maxColumnCount = 0;
            for (int rowIndex = 0; rowIndex < patternRows.Count; rowIndex++)
            {
                if (patternRows[rowIndex].Count > maxColumnCount)
                {
                    maxColumnCount = patternRows[rowIndex].Count;
                }
            }

            float globalCenterOffset = (maxColumnCount - 1) * tileSpacing * 0.5f;

            for (int rowIndex = 0; rowIndex < patternRows.Count; rowIndex++)
            {
                List<PatternCell> row = patternRows[rowIndex];
                float y = (patternRows.Count - 1 - rowIndex) * tileSpacing;

                for (int colIndex = 0; colIndex < row.Count; colIndex++)
                {
                    BlockColor color = row[colIndex].Color;
                    if (color == BlockColor.None)
                    {
                        continue;
                    }

                    float x = (colIndex * tileSpacing) - globalCenterOffset;
                    Vector3 expectedPosition = new Vector3(x, y, 0f);
                    TileVisualEntry matched = FindAndTakeClosestVisual(color, expectedPosition);
                    if (matched == null || matched.Renderer == null)
                    {
                        continue;
                    }

                    var occurrenceKey = (Column: colIndex, Color: color);
                    if (!occurrenceCounters.TryGetValue(occurrenceKey, out int occurrence))
                    {
                        occurrence = 0;
                    }

                    occurrenceCounters[occurrenceKey] = occurrence + 1;
                    TileStableKey stableKey = new TileStableKey(colIndex, color, occurrence);

                    if (!result.TryGetValue(stableKey, out Queue<TileVisualEntry> queue))
                    {
                        queue = new Queue<TileVisualEntry>();
                        result.Add(stableKey, queue);
                    }

                    queue.Enqueue(matched);
                }
            }

            return result;
        }

        private TileVisualEntry FindAndTakeClosestVisual(BlockColor color, Vector3 expectedPosition)
        {
            int matchedIndex = -1;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < tileVisuals.Count; i++)
            {
                TileVisualEntry entry = tileVisuals[i];
                if (entry == null || entry.Renderer == null)
                {
                    continue;
                }

                if (entry.Renderer.color != MapColor(color))
                {
                    continue;
                }

                float distance = Vector3.SqrMagnitude(entry.Renderer.transform.localPosition - expectedPosition);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    matchedIndex = i;
                }
            }

            if (matchedIndex < 0)
            {
                return null;
            }

            TileVisualEntry matchedEntry = tileVisuals[matchedIndex];
            tileVisuals.RemoveAt(matchedIndex);
            return matchedEntry;
        }

        private void DestroyRemainingOldVisuals(Dictionary<TileStableKey, Queue<TileVisualEntry>> oldVisualsByKey, List<TileVisualEntry> unmatchedOldVisuals)
        {
            if (oldVisualsByKey == null)
            {
                return;
            }

            foreach (KeyValuePair<TileStableKey, Queue<TileVisualEntry>> pair in oldVisualsByKey)
            {
                Queue<TileVisualEntry> queue = pair.Value;
                while (queue.Count > 0)
                {
                    TileVisualEntry oldEntry = queue.Dequeue();
                    if (oldEntry != null && oldEntry.Renderer != null)
                    {
                        Destroy(oldEntry.Renderer.gameObject);
                    }
                }
            }

            if (unmatchedOldVisuals == null)
            {
                return;
            }

            for (int i = 0; i < unmatchedOldVisuals.Count; i++)
            {
                TileVisualEntry leftover = unmatchedOldVisuals[i];
                if (leftover != null && leftover.Renderer != null)
                {
                    Destroy(leftover.Renderer.gameObject);
                }
            }
        }

        private void SnapVisualsToTargetPositions()
        {
            for (int i = 0; i < tileVisuals.Count; i++)
            {
                TileVisualEntry entry = tileVisuals[i];
                if (entry == null || entry.Renderer == null)
                {
                    continue;
                }

                entry.Renderer.transform.localPosition = entry.TargetLocalPosition;
                entry.Renderer.sortingOrder = entry.SortingOrder;
            }
        }

        private IEnumerator AnimateFallCoroutine()
        {
            List<Vector3> startPositions = new List<Vector3>(tileVisuals.Count);
            List<float> durations = new List<float>(tileVisuals.Count);
            float maxDuration = 0f;

            for (int i = 0; i < tileVisuals.Count; i++)
            {
                TileVisualEntry entry = tileVisuals[i];
                Vector3 startPosition = entry != null && entry.Renderer != null ? entry.Renderer.transform.localPosition : Vector3.zero;
                startPositions.Add(startPosition);
                Vector3 targetPosition = entry != null ? entry.TargetLocalPosition : startPosition;

                float distanceInCells = tileSpacing > 0f
                    ? Mathf.Abs(targetPosition.y - startPosition.y) / tileSpacing
                    : 0f;
                float duration = baseFallDuration + (distanceInCells * perCellFallTime);
                durations.Add(duration);

                if (duration > maxDuration)
                {
                    maxDuration = duration;
                }
            }

            if (maxDuration <= 0f)
            {
                SnapVisualsToTargetPositions();
                fallAnimationCoroutine = null;
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < maxDuration)
            {
                elapsed += Time.deltaTime;

                for (int i = 0; i < tileVisuals.Count; i++)
                {
                    TileVisualEntry entry = tileVisuals[i];
                    if (entry == null || entry.Renderer == null)
                    {
                        continue;
                    }

                    float tileDuration = durations[i];
                    float t = tileDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / tileDuration);
                    t = t * t * (3f - (2f * t));

                    entry.Renderer.transform.localPosition = Vector3.Lerp(startPositions[i], entry.TargetLocalPosition, t);
                    entry.Renderer.sortingOrder = entry.SortingOrder;
                }

                yield return null;
            }

            SnapVisualsToTargetPositions();
            fallAnimationCoroutine = null;
        }

        private void StopFallAnimationIfRunning()
        {
            if (fallAnimationCoroutine != null)
            {
                StopCoroutine(fallAnimationCoroutine);
                fallAnimationCoroutine = null;
            }
        }

        private SpriteRenderer CreateTileRenderer(Transform root, BlockColor color)
        {
            GameObject visualObject = null;
            if (tileVisualPrefab != null)
            {
                visualObject = Instantiate(tileVisualPrefab, root);
            }

            if (visualObject == null)
            {
                visualObject = new GameObject("PatternTileVisual");
                visualObject.transform.SetParent(root, false);
            }

            visualObject.name = "PatternTileVisual";

            Transform visualTransform = visualObject.transform;
            visualTransform.SetParent(root, false);
            visualTransform.localPosition = Vector3.zero;
            visualTransform.localRotation = Quaternion.identity;
            visualTransform.localScale = Vector3.one;

            SpriteRenderer renderer = visualObject.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = visualObject.AddComponent<SpriteRenderer>();
            }

            if (renderer.sprite == null)
            {
                renderer.sprite = GetSolidSquareSprite();
            }

            renderer.drawMode = SpriteDrawMode.Simple;
            renderer.color = MapColor(color);

            return renderer;
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
                Transform child = root.GetChild(i);
                if (child != null && child.name == "PatternTileVisual")
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private Transform GetTileRoot()
        {
            return tileRoot != null ? tileRoot : transform;
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
                name = "PatternGeneratedTexture"
            };

            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            cachedSolidSquareSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            cachedSolidSquareSprite.name = "PatternGeneratedSprite";
            return cachedSolidSquareSprite;
        }

        private Vector3 GetCompensatedVisualScale(Transform parent)
        {
            Vector3 parentLossyScale = parent != null ? parent.lossyScale : Vector3.one;

            float scaleX = SafeDiv(visualScale, parentLossyScale.x);
            float scaleY = SafeDiv(visualScale, parentLossyScale.y);

            return new Vector3(scaleX, scaleY, 1f);
        }

        private static float SafeDiv(float numerator, float denominator)
        {
            return Mathf.Abs(denominator) <= 0.0001f ? numerator : numerator / denominator;
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

        private void OnDisable()
        {
            StopFallAnimationIfRunning();
            ClearAllVisuals();
        }

        private void OnDestroy()
        {
            StopFallAnimationIfRunning();
            ClearAllVisuals();
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
