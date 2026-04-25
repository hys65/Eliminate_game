using System;
using SCG = System.Collections.Generic;
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
        [SerializeField] private int sortingOrderBase = 300;
        [SerializeField, Min(1)] private int sortingOrderRowStride = 20;

        [Header("Pattern Fall Animation")]
        [SerializeField, Min(0.15f)] private float fallDuration = 0.2f;
        [SerializeField, Min(1f)] private float minColumnSpawnRows = 4f;
        [SerializeField, Min(0f)] private float perColumnDelay = 0.02f;

        private readonly SCG.List<SCG.List<PatternCell>> patternRows = new SCG.List<SCG.List<PatternCell>>();
        private readonly SCG.List<TileVisualEntry> tileVisuals = new SCG.List<TileVisualEntry>();
        private Coroutine fallAnimationCoroutine;

        private static Sprite cachedSolidSquareSprite;

        private sealed class TileVisualEntry
        {
            public SpriteRenderer Renderer;
            public Vector3 StartLocalPosition;
            public Vector3 TargetLocalPosition;
            public int ColumnIndex;
        }

        public bool IsEmpty => GetBottomRowIndex() < 0;

        public void Initialize(SCG.IReadOnlyList<SCG.IReadOnlyList<BlockColor>> rows)
        {
            patternRows.Clear();

            foreach (SCG.IReadOnlyList<BlockColor> row in rows)
            {
                var builtRow = new SCG.List<PatternCell>();

                foreach (BlockColor color in row)
                {
                    builtRow.Add(new PatternCell(color));
                }

                patternRows.Add(builtRow);
            }

            RefreshVisuals(false);
            Debug.Log($"Pattern initialized. Rows={patternRows.Count}, Bottom=[{string.Join(",", GetBottomRowColors())}]");
        }

        public SCG.IReadOnlyList<BlockColor> GetBottomRowColors()
        {
            int bottomIndex = GetBottomRowIndex();
            if (bottomIndex < 0)
            {
                return Array.Empty<BlockColor>();
            }

            SCG.List<PatternCell> bottomRow = patternRows[bottomIndex];
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

            SCG.List<PatternCell> bottomRow = patternRows[bottomIndex];
            SCG.List<int> colorIndices = new SCG.List<int>();

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
                RefreshVisuals(true);
                Debug.Log($"Pattern Case A resolved for {color}. Removed={colorIndices.Count} from bottom row.");
                return PatternResolveResult.CaseA(colorIndices.Count);
            }

            SCG.List<int> firstThree = colorIndices.Take(3).ToList();
            SetCellsToNone(bottomRow, firstThree);
            ApplyColumnGravity();
            CollapseIfNeeded();
            RefreshVisuals(true);
            Debug.Log($"Pattern Case B resolved for {color}. Removed=3 from bottom row (left-to-right).");
            return PatternResolveResult.CaseB(3);
        }

        private int GetBottomRowIndex()
        {
            for (int rowIndex = patternRows.Count - 1; rowIndex >= 0; rowIndex--)
            {
                SCG.List<PatternCell> row = patternRows[rowIndex];
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

        private static void SetCellsToNone(SCG.List<PatternCell> row, SCG.List<int> indices)
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

            SCG.List<PatternCell> row = patternRows[rowIndex];
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

        private static bool IsAllNoneRow(SCG.List<PatternCell> row)
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

        private void RefreshVisuals(bool animateFall)
        {
            if (fallAnimationCoroutine != null)
            {
                StopCoroutine(fallAnimationCoroutine);
                fallAnimationCoroutine = null;
            }

            ClearAllVisuals();

            Transform root = GetTileRoot();
            if (root == null)
            {
                return;
            }

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
                SCG.List<PatternCell> row = patternRows[rowIndex];
                float targetY = (patternRows.Count - 1 - rowIndex) * tileSpacing;

                for (int colIndex = 0; colIndex < row.Count; colIndex++)
                {
                    BlockColor color = row[colIndex].Color;
                    if (color == BlockColor.None)
                    {
                        continue;
                    }

                    float targetX = (colIndex * tileSpacing) - globalCenterOffset;
                    Vector3 targetLocalPosition = new Vector3(targetX, targetY, 0f);
                    float columnSpawnOffset = GetColumnSpawnOffset(colIndex);
                    Vector3 startLocalPosition = new Vector3(targetX, targetY + columnSpawnOffset, 0f);
                    int sortingOrder = sortingOrderBase + ((patternRows.Count - 1 - rowIndex) * sortingOrderRowStride) + colIndex;

                    SpriteRenderer renderer = CreateTileRenderer(root, color);
                    if (renderer == null)
                    {
                        continue;
                    }

                    Transform tileTransform = renderer.transform;
                    tileTransform.localPosition = animateFall ? startLocalPosition : targetLocalPosition;
                    tileTransform.localRotation = Quaternion.identity;
                    tileTransform.localScale = GetCompensatedVisualScale(tileTransform.parent);
                    renderer.sortingOrder = sortingOrder;

                    tileVisuals.Add(new TileVisualEntry
                    {
                        Renderer = renderer,
                        StartLocalPosition = startLocalPosition,
                        TargetLocalPosition = targetLocalPosition,
                        ColumnIndex = colIndex
                    });
                }
            }

            if (animateFall && tileVisuals.Count > 0)
            {
                fallAnimationCoroutine = StartCoroutine(AnimateTileFallCoroutine());
            }
        }

        private float GetColumnSpawnOffset(int colIndex)
        {
            int columnHeight = 0;
            for (int rowIndex = 0; rowIndex < patternRows.Count; rowIndex++)
            {
                if (!TryGetCell(rowIndex, colIndex, out PatternCell cell))
                {
                    continue;
                }

                if (cell.Color != BlockColor.None)
                {
                    columnHeight++;
                }
            }

            float spawnRows = Mathf.Max(minColumnSpawnRows, columnHeight);
            return spawnRows * tileSpacing;
        }

        private System.Collections.IEnumerator AnimateTileFallCoroutine()
        {
            float duration = Mathf.Clamp(fallDuration, 0.15f, 0.25f);
            float columnDelay = Mathf.Max(0f, perColumnDelay);
            int maxColumnIndex = 0;

            for (int i = 0; i < tileVisuals.Count; i++)
            {
                TileVisualEntry entry = tileVisuals[i];
                if (entry != null && entry.ColumnIndex > maxColumnIndex)
                {
                    maxColumnIndex = entry.ColumnIndex;
                }
            }

            float totalDuration = duration + (maxColumnIndex * columnDelay);
            float elapsed = 0f;

            while (elapsed < totalDuration)
            {
                elapsed += Time.deltaTime;

                for (int i = 0; i < tileVisuals.Count; i++)
                {
                    TileVisualEntry entry = tileVisuals[i];
                    if (entry == null || entry.Renderer == null)
                    {
                        continue;
                    }

                    float startDelay = entry.ColumnIndex * columnDelay;
                    float normalized = Mathf.Clamp01((elapsed - startDelay) / duration);
                    float eased = normalized * normalized * (3f - 2f * normalized);
                    entry.Renderer.transform.localPosition = Vector3.LerpUnclamped(entry.StartLocalPosition, entry.TargetLocalPosition, eased);
                }

                yield return null;
            }

            for (int i = 0; i < tileVisuals.Count; i++)
            {
                TileVisualEntry entry = tileVisuals[i];
                if (entry == null || entry.Renderer == null)
                {
                    continue;
                }

                entry.Renderer.transform.localPosition = entry.TargetLocalPosition;
            }

            fallAnimationCoroutine = null;
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
            if (fallAnimationCoroutine != null)
            {
                StopCoroutine(fallAnimationCoroutine);
                fallAnimationCoroutine = null;
            }

            ClearAllVisuals();
        }

        private void OnDestroy()
        {
            if (fallAnimationCoroutine != null)
            {
                StopCoroutine(fallAnimationCoroutine);
                fallAnimationCoroutine = null;
            }

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
