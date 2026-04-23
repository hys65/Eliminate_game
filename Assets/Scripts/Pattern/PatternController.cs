using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EliminateGame.Pattern
{
    public class PatternController : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private Transform tileRoot;
        [SerializeField] private GameObject tileVisualPrefab;
        [SerializeField] private float spacing = 1.0f;

        private readonly List<List<PatternCell>> patternRows = new List<List<PatternCell>>();
        private readonly List<SpriteRenderer> tileVisuals = new List<SpriteRenderer>();

        private static Sprite cachedSolidSquareSprite;

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

            RefreshVisuals();

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
                RemoveCellsAtIndices(bottomRow, colorIndices);
                CollapseIfNeeded();
                RefreshVisuals();

                Debug.Log($"Pattern Case A resolved for {color}. Removed={colorIndices.Count} from bottom row.");
                return PatternResolveResult.CaseA(colorIndices.Count);
            }

            List<int> firstThree = colorIndices.Take(3).ToList();
            RemoveCellsAtIndices(bottomRow, firstThree);
            CollapseIfNeeded();
            RefreshVisuals();

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
            for (int i = patternRows.Count - 1; i >= 0; i--)
            {
                if (patternRows[i].Count == 0)
                {
                    patternRows.RemoveAt(i);
                }
            }

            Debug.Log($"Pattern collapse check complete. Rows={patternRows.Count}, Bottom=[{string.Join(",", GetBottomRowColors())}]");
        }

        private void RefreshVisuals()
        {
            ClearAllVisuals();

            Transform root = GetTileRoot();
            if (root == null)
            {
                return;
            }

            if (patternRows.Count == 0)
            {
                return;
            }

            for (int rowIndex = 0; rowIndex < patternRows.Count; rowIndex++)
            {
                List<PatternCell> row = patternRows[rowIndex];
                float startOffsetX = (row.Count - 1) * spacing * 0.5f;
                float localY = -rowIndex * spacing;

                for (int columnIndex = 0; columnIndex < row.Count; columnIndex++)
                {
                    PatternCell cell = row[columnIndex];
                    SpriteRenderer renderer = CreateVisualForColor(cell.Color, rowIndex, columnIndex);
                    if (renderer == null)
                    {
                        continue;
                    }

                    renderer.transform.localPosition = new Vector3((columnIndex * spacing) - startOffsetX, localY, 0f);
                    renderer.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
                    tileVisuals.Add(renderer);
                }
            }
        }

        private SpriteRenderer CreateVisualForColor(BlockColor color, int rowIndex, int columnIndex)
        {
            Transform root = GetTileRoot();
            if (root == null)
            {
                return null;
            }

            GameObject visualObject = null;

            if (tileVisualPrefab != null)
            {
                visualObject = Instantiate(tileVisualPrefab, root);
            }

            if (visualObject == null)
            {
                visualObject = new GameObject($"PatternTile_{rowIndex}_{columnIndex}_{color}");
                visualObject.transform.SetParent(root, false);
            }
            else
            {
                visualObject.name = $"PatternTile_{rowIndex}_{columnIndex}_{color}";
            }

            SpriteRenderer renderer = visualObject.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = visualObject.AddComponent<SpriteRenderer>();
            }

            ForceSolidSquareStyle(renderer, color);
            return renderer;
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

        private void ClearAllVisuals()
        {
            for (int i = 0; i < tileVisuals.Count; i++)
            {
                if (tileVisuals[i] != null)
                {
                    Destroy(tileVisuals[i].gameObject);
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