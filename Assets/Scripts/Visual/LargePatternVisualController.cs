using System;
using System.Collections.Generic;
using EliminateGame.Pattern;
using UnityEngine;

namespace EliminateGame.Visual
{
    public class LargePatternVisualController : MonoBehaviour
    {
        private const string VisualCellName = "LargePatternVisualCell";

        [Header("Visual-only config")]
        [SerializeField] private LargePatternVisualConfig visualConfig;
        [SerializeField] private Transform tileRoot;
        [SerializeField] private GameObject tileVisualPrefab;
        [SerializeField, Min(0f)] private float spacing = 0.01f;
        [SerializeField] private int sortingOrderBase = 500;
        [SerializeField] private bool buildOnStart = true;

        private static Sprite cachedSolidSquareSprite;
        private sealed class VisualCellState
        {
            public int X;
            public int Y;
            public int PaletteIndex;
            public SpriteRenderer Renderer;
            public bool IsVisible;
        }

        private readonly List<SpriteRenderer> visualCells = new List<SpriteRenderer>();
        private readonly List<VisualCellState> visualCellStates = new List<VisualCellState>();
        private readonly Dictionary<Vector2Int, VisualCellState> visualCellLookup = new Dictionary<Vector2Int, VisualCellState>();

        private void Start()
        {
            if (buildOnStart)
            {
                BuildVisualGrid();
            }
        }

        [ContextMenu("Build Visual Grid")]
        public void BuildVisualGrid()
        {
            ClearVisualGrid();

            if (visualConfig == null || !visualConfig.ValidateSize())
            {
                Debug.LogWarning("LargePatternVisualController skipped build because visualConfig is missing or has an invalid cell count.", this);
                return;
            }

            Transform root = GetTileRoot();
            float step = visualConfig.CellSize + spacing;
            float xOffset = (visualConfig.Width - 1) * step * 0.5f;
            float yOffset = (visualConfig.Height - 1) * step * 0.5f;

            int nonNoneCellCount = 0;

            for (int y = 0; y < visualConfig.Height; y++)
            {
                for (int x = 0; x < visualConfig.Width; x++)
                {
                    if (!TryGetVisualCellColor(x, y, out Color visualColor, out int paletteIndex))
                    {
                        continue;
                    }

                    nonNoneCellCount++;
                    SpriteRenderer renderer = CreateVisualCell(root, visualColor);
                    if (renderer == null)
                    {
                        continue;
                    }

                    Transform cellTransform = renderer.transform;
                    float localX = (x * step) - xOffset;
                    float localY = yOffset - (y * step);
                    cellTransform.localPosition = new Vector3(localX, localY, 0f);
                    cellTransform.localRotation = Quaternion.identity;
                    cellTransform.localScale = GetCompensatedCellScale(cellTransform.parent, visualConfig.CellSize);
                    renderer.sortingOrder = sortingOrderBase + ((visualConfig.Height - 1 - y) * visualConfig.Width) + x;

                    VisualCellState state = new VisualCellState
                    {
                        X = x,
                        Y = y,
                        PaletteIndex = paletteIndex,
                        Renderer = renderer,
                        IsVisible = true
                    };
                    visualCells.Add(renderer);
                    visualCellStates.Add(state);
                    visualCellLookup[new Vector2Int(x, y)] = state;
                }
            }

            Debug.Log($"[LargePatternVisual] Built visual grid Width={visualConfig.Width} Height={visualConfig.Height} NonNoneCells={nonNoneCellCount}", this);
        }


        public bool IsCellVisible(int x, int y)
        {
            return visualCellLookup.TryGetValue(new Vector2Int(x, y), out VisualCellState state) && state != null && state.IsVisible && state.Renderer != null && state.Renderer.enabled;
        }

        public void HideCell(int x, int y)
        {
            if (visualCellLookup.TryGetValue(new Vector2Int(x, y), out VisualCellState state) && state != null && state.Renderer != null)
            {
                state.Renderer.enabled = false;
                state.IsVisible = false;
            }
        }

        public void HideCells(IEnumerable<Vector2Int> coordinates)
        {
            if (coordinates == null)
            {
                return;
            }

            foreach (Vector2Int coordinate in coordinates)
            {
                HideCell(coordinate.x, coordinate.y);
            }
        }

        public int HideCellsByPaletteIndices(IReadOnlyCollection<int> paletteIndices, int maxCount, int deterministicSeed, bool preferBottomToTop = false)
        {
            if (paletteIndices == null || paletteIndices.Count == 0 || maxCount <= 0)
            {
                return 0;
            }

            HashSet<int> targetIndices = new HashSet<int>(paletteIndices);
            List<VisualCellState> candidates = new List<VisualCellState>();
            for (int i = 0; i < visualCellStates.Count; i++)
            {
                VisualCellState state = visualCellStates[i];
                if (state != null && state.IsVisible && state.Renderer != null && targetIndices.Contains(state.PaletteIndex))
                {
                    candidates.Add(state);
                }
            }

            candidates.Sort((left, right) => CompareDeterministicCellOrder(left, right, deterministicSeed, preferBottomToTop));
            int hideCount = Mathf.Min(maxCount, candidates.Count);
            for (int i = 0; i < hideCount; i++)
            {
                candidates[i].Renderer.enabled = false;
                candidates[i].IsVisible = false;
            }

            return hideCount;
        }

        public int HideAnyVisibleCells(int maxCount, int deterministicSeed, bool preferBottomToTop = false)
        {
            if (maxCount <= 0)
            {
                return 0;
            }

            List<VisualCellState> candidates = new List<VisualCellState>();
            for (int i = 0; i < visualCellStates.Count; i++)
            {
                VisualCellState state = visualCellStates[i];
                if (state != null && state.IsVisible && state.Renderer != null)
                {
                    candidates.Add(state);
                }
            }

            candidates.Sort((left, right) => CompareDeterministicCellOrder(left, right, deterministicSeed, preferBottomToTop));
            int hideCount = Mathf.Min(maxCount, candidates.Count);
            for (int i = 0; i < hideCount; i++)
            {
                candidates[i].Renderer.enabled = false;
                candidates[i].IsVisible = false;
            }

            return hideCount;
        }

        public void ResetVisualState()
        {
            SetAllVisualCellsEnabled(true);
        }

        public void HideAllCells()
        {
            SetAllVisualCellsEnabled(false);
        }

        private void SetAllVisualCellsEnabled(bool enabled)
        {
            for (int i = 0; i < visualCellStates.Count; i++)
            {
                VisualCellState state = visualCellStates[i];
                if (state != null)
                {
                    state.IsVisible = enabled;
                    if (state.Renderer != null)
                    {
                        state.Renderer.enabled = enabled;
                    }
                }
            }
        }

        [ContextMenu("Clear Visual Grid")]
        public void ClearVisualGrid()
        {
            for (int i = 0; i < visualCells.Count; i++)
            {
                SpriteRenderer renderer = visualCells[i];
                if (renderer != null)
                {
                    DestroySafe(renderer.gameObject);
                }
            }

            visualCells.Clear();
            visualCellStates.Clear();
            visualCellLookup.Clear();

            Transform root = GetTileRoot();
            if (root == null)
            {
                return;
            }

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (child != null && child.name == VisualCellName)
                {
                    DestroySafe(child.gameObject);
                }
            }
        }

        private bool TryGetVisualCellColor(int x, int y, out Color visualColor, out int paletteIndex)
        {
            visualColor = Color.clear;
            paletteIndex = LargePatternVisualConfig.TransparentPaletteIndex;
            if (visualConfig == null)
            {
                return false;
            }

            if (visualConfig.HasValidPaletteData)
            {
                return visualConfig.TryGetPaletteCellColor(x, y, out visualColor, out paletteIndex);
            }

            BlockColor legacyColor = visualConfig.GetCell(x, y);
            if (legacyColor == BlockColor.None)
            {
                return false;
            }

            visualColor = MapColor(legacyColor);
            paletteIndex = -1;
            return visualColor.a > 0.001f;
        }

        private SpriteRenderer CreateVisualCell(Transform root, Color color)
        {
            GameObject visualObject = tileVisualPrefab != null
                ? Instantiate(tileVisualPrefab, root)
                : new GameObject(VisualCellName);

            if (visualObject == null)
            {
                return null;
            }

            visualObject.name = VisualCellName;
            visualObject.transform.SetParent(root, false);
            StripNonVisualComponents(visualObject);

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
            renderer.color = color;
            return renderer;
        }

        private static void StripNonVisualComponents(GameObject rootObject)
        {
            Component[] components = rootObject.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null)
                {
                    continue;
                }

                Type componentType = component.GetType();
                if (componentType == typeof(Transform) || componentType == typeof(SpriteRenderer))
                {
                    continue;
                }

                if (component is Behaviour behaviour)
                {
                    behaviour.enabled = false;
                }

                if (component is Collider2D collider2D)
                {
                    collider2D.enabled = false;
                }

                DestroySafe(component);
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

            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "LargePatternVisualGeneratedTexture"
            };

            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            cachedSolidSquareSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            cachedSolidSquareSprite.name = "LargePatternVisualGeneratedSprite";
            return cachedSolidSquareSprite;
        }

        private static Vector3 GetCompensatedCellScale(Transform parent, float cellSize)
        {
            Vector3 parentLossyScale = parent != null ? parent.lossyScale : Vector3.one;
            float scaleX = SafeDiv(cellSize, parentLossyScale.x);
            float scaleY = SafeDiv(cellSize, parentLossyScale.y);
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

        private static int CompareDeterministicCellOrder(VisualCellState left, VisualCellState right, int seed, bool preferBottomToTop)
        {
            if (preferBottomToTop)
            {
                int yCompare = right.Y.CompareTo(left.Y);
                if (yCompare != 0)
                {
                    return yCompare;
                }
            }

            int hashCompare = GetDeterministicCellHash(left, seed).CompareTo(GetDeterministicCellHash(right, seed));
            if (hashCompare != 0)
            {
                return hashCompare;
            }

            int yFallback = left.Y.CompareTo(right.Y);
            return yFallback != 0 ? yFallback : left.X.CompareTo(right.X);
        }

        private static int GetDeterministicCellHash(VisualCellState state, int seed)
        {
            unchecked
            {
                int hash = seed;
                hash = (hash * 397) ^ state.X;
                hash = (hash * 397) ^ state.Y;
                hash = (hash * 397) ^ state.PaletteIndex;
                return hash & int.MaxValue;
            }
        }

        private static void DestroySafe(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
