using System;
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
        [SerializeField] private int sortingOrderBase = 300;
        [SerializeField, Min(1)] private int sortingOrderRowStride = 20;

        private readonly List<List<PatternCell>> patternRows = new List<List<PatternCell>>();
        private readonly List<TileVisualEntry> tileVisuals = new List<TileVisualEntry>();

        private static Sprite cachedSolidSquareSprite;

        private sealed class TileVisualEntry
        {
            public SpriteRenderer Renderer;
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

            RefreshVisuals();
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
                CollapseIfNeeded();
                RefreshVisuals();
                Debug.Log($"Pattern Case A resolved for {color}. Removed={colorIndices.Count} from bottom row.");
                return PatternResolveResult.CaseA(colorIndices.Count);
            }

            List<int> firstThree = colorIndices.Take(3).ToList();
            SetCellsToNone(bottomRow, firstThree);
            CollapseIfNeeded();
            RefreshVisuals();
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

        private void RefreshVisuals()
        {
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
                List<PatternCell> row = patternRows[rowIndex];
                float y = (patternRows.Count - 1 - rowIndex) * tileSpacing;

                for (int colIndex = 0; colIndex < row.Count; colIndex++)
                {
                    BlockColor color = row[colIndex].Color;
                    if (color == BlockColor.None)
                    {
                        continue;
                    }

                    SpriteRenderer renderer = CreateTileRenderer(root, color);
                    if (renderer == null)
                    {
                        continue;
                    }

                    Transform tileTransform = renderer.transform;
                    float x = (colIndex * tileSpacing) - globalCenterOffset;
                    tileTransform.localPosition = new Vector3(x, y, 0f);
                    tileTransform.localRotation = Quaternion.identity;
                    tileTransform.localScale = GetCompensatedVisualScale(tileTransform.parent);

                    renderer.sortingOrder = sortingOrderBase + ((patternRows.Count - 1 - rowIndex) * sortingOrderRowStride) + colIndex;

                    tileVisuals.Add(new TileVisualEntry
                    {
                        Renderer = renderer
                    });
                }
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
            ClearAllVisuals();
        }

        private void OnDestroy()
        {
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
