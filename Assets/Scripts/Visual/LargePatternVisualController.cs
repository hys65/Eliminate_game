using System;
using System.Collections;
using System.Collections.Generic;
using EliminateGame.Pattern;
using UnityEngine;

namespace EliminateGame.Visual
{
    public class LargePatternVisualController : MonoBehaviour
    {
        private const string VisualCellName = "LargePatternVisualCell";
        private const string BackgroundName = "LargePatternVisualBackground";

        [Header("Visual-only config")]
        [SerializeField] private LargePatternVisualConfig visualConfig;
        [SerializeField] private Transform tileRoot;
        [SerializeField] private GameObject tileVisualPrefab;
        [SerializeField, Min(0f)] private float spacing = 0.01f;
        [SerializeField] private int sortingOrderBase = 500;
        [SerializeField] private bool buildOnStart = true;

        [Header("Visual polish")]
        [SerializeField] private bool backgroundEnabled = false;
        [SerializeField] private Color backgroundColor = new Color(0.96f, 0.92f, 0.84f, 1f);
        [SerializeField, Min(0f)] private float backgroundPadding = 0.2f;
        [SerializeField] private bool hideAnimationEnabled = true;
        [SerializeField, Min(0.01f)] private float hideAnimationDuration = 0.12f;
        [SerializeField, Range(0f, 1f)] private float hideAnimationEndScaleMultiplier = 0.65f;

        private static Sprite cachedSolidSquareSprite;
        private sealed class VisualCellState
        {
            public int DataX;
            public int DataY;
            public int RenderX;
            public int RenderY;
            public int PaletteIndex;
            public SpriteRenderer Renderer;
            public bool IsVisible;
            public Color OriginalColor;
            public Vector3 OriginalScale;
            public Coroutine HideCoroutine;
        }

        private readonly List<SpriteRenderer> visualCells = new List<SpriteRenderer>();
        private readonly List<VisualCellState> visualCellStates = new List<VisualCellState>();
        private readonly Dictionary<Vector2Int, VisualCellState> visualCellLookup = new Dictionary<Vector2Int, VisualCellState>();
        private SpriteRenderer backgroundRenderer;

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

            for (int dataY = 0; dataY < visualConfig.Height; dataY++)
            {
                for (int dataX = 0; dataX < visualConfig.Width; dataX++)
                {
                    if (!TryGetVisualCellColor(dataX, dataY, out Color visualColor, out int paletteIndex))
                    {
                        continue;
                    }

                    nonNoneCellCount++;
                    SpriteRenderer renderer = CreateVisualCell(root, visualColor);
                    if (renderer == null)
                    {
                        continue;
                    }

                    int renderX = dataX;
                    int renderY = ConvertDataYToRenderRow(dataY);

                    Transform cellTransform = renderer.transform;
                    float localX = (renderX * step) - xOffset;
                    float localY = yOffset - (renderY * step);
                    cellTransform.localPosition = new Vector3(localX, localY, 0f);
                    cellTransform.localRotation = Quaternion.identity;
                    cellTransform.localScale = GetCompensatedCellScale(cellTransform.parent, visualConfig.CellSize);
                    renderer.sortingOrder = sortingOrderBase + ((visualConfig.Height - 1 - renderY) * visualConfig.Width) + renderX;

                    VisualCellState state = new VisualCellState
                    {
                        DataX = dataX,
                        DataY = dataY,
                        RenderX = renderX,
                        RenderY = renderY,
                        PaletteIndex = paletteIndex,
                        Renderer = renderer,
                        IsVisible = true,
                        OriginalColor = renderer.color,
                        OriginalScale = cellTransform.localScale
                    };
                    visualCells.Add(renderer);
                    visualCellStates.Add(state);
                    visualCellLookup[new Vector2Int(dataX, dataY)] = state;
                }
            }

            EnsureBackground(root, step, xOffset, yOffset);

            Debug.Log($"[LargePatternVisual] Built visual grid Width={visualConfig.Width} Height={visualConfig.Height} NonNoneCells={nonNoneCellCount}", this);
        }


        public bool IsCellVisible(int x, int y)
        {
            return visualCellLookup.TryGetValue(new Vector2Int(x, y), out VisualCellState state) && state != null && state.IsVisible && state.Renderer != null && state.Renderer.enabled;
        }

        public void HideCell(int x, int y)
        {
            if (!visualCellLookup.TryGetValue(new Vector2Int(x, y), out VisualCellState state))
            {
                return;
            }

            HideState(state);
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
                HideState(candidates[i]);
            }

            return hideCount;
        }

        public int HideCellsByPaletteIndicesInRegion(
            IReadOnlyCollection<int> paletteIndices,
            int maxCount,
            int deterministicSeed,
            int startX,
            int endX,
            int startY,
            int endY,
            bool preferBottomToTop = false)
        {
            if (paletteIndices == null || paletteIndices.Count == 0 || maxCount <= 0 || visualConfig == null)
            {
                return 0;
            }

            ClampRegionToVisualConfig(ref startX, ref endX, ref startY, ref endY);

            HashSet<int> targetIndices = new HashSet<int>(paletteIndices);
            List<VisualCellState> candidates = new List<VisualCellState>();
            for (int i = 0; i < visualCellStates.Count; i++)
            {
                VisualCellState state = visualCellStates[i];
                if (state != null
                    && state.IsVisible
                    && state.Renderer != null
                    && state.DataX >= startX
                    && state.DataX <= endX
                    && state.DataY >= startY
                    && state.DataY <= endY
                    && targetIndices.Contains(state.PaletteIndex))
                {
                    candidates.Add(state);
                }
            }

            candidates.Sort((left, right) => CompareDeterministicCellOrder(left, right, deterministicSeed, preferBottomToTop));
            int hideCount = Mathf.Min(maxCount, candidates.Count);
            for (int i = 0; i < hideCount; i++)
            {
                HideState(candidates[i]);
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
                HideState(candidates[i]);
            }

            return hideCount;
        }

        public int HideAnyVisibleCellsInRegion(
            int maxCount,
            int deterministicSeed,
            int startX,
            int endX,
            int startY,
            int endY,
            bool preferBottomToTop = false)
        {
            if (maxCount <= 0 || visualConfig == null)
            {
                return 0;
            }

            ClampRegionToVisualConfig(ref startX, ref endX, ref startY, ref endY);

            List<VisualCellState> candidates = new List<VisualCellState>();
            for (int i = 0; i < visualCellStates.Count; i++)
            {
                VisualCellState state = visualCellStates[i];
                if (state != null
                    && state.IsVisible
                    && state.Renderer != null
                    && state.DataX >= startX
                    && state.DataX <= endX
                    && state.DataY >= startY
                    && state.DataY <= endY)
                {
                    candidates.Add(state);
                }
            }

            candidates.Sort((left, right) => CompareDeterministicCellOrder(left, right, deterministicSeed, preferBottomToTop));
            int hideCount = Mathf.Min(maxCount, candidates.Count);
            for (int i = 0; i < hideCount; i++)
            {
                HideState(candidates[i]);
            }

            return hideCount;
        }

        public void ResetVisualState()
        {
            for (int i = 0; i < visualCellStates.Count; i++)
            {
                VisualCellState state = visualCellStates[i];
                if (state == null)
                {
                    continue;
                }

                StopHideCoroutine(state);
                state.IsVisible = true;
                if (state.Renderer != null)
                {
                    state.Renderer.enabled = true;
                    state.Renderer.color = state.OriginalColor;
                    state.Renderer.transform.localScale = state.OriginalScale;
                }
            }

            if (backgroundEnabled && backgroundRenderer != null)
            {
                backgroundRenderer.enabled = true;
            }
        }

        public void HideAllCells()
        {
            for (int i = 0; i < visualCellStates.Count; i++)
            {
                VisualCellState state = visualCellStates[i];
                if (state == null)
                {
                    continue;
                }

                StopHideCoroutine(state);
                state.IsVisible = false;
                if (state.Renderer != null)
                {
                    state.Renderer.enabled = false;
                    state.Renderer.color = state.OriginalColor;
                    state.Renderer.transform.localScale = state.OriginalScale;
                }
            }

            if (backgroundRenderer != null)
            {
                backgroundRenderer.enabled = false;
            }
        }

        [ContextMenu("Clear Visual Grid")]
        public void ClearVisualGrid()
        {
            for (int i = 0; i < visualCellStates.Count; i++)
            {
                StopHideCoroutine(visualCellStates[i]);
            }

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
            backgroundRenderer = null;

            Transform root = GetTileRoot();
            if (root == null)
            {
                return;
            }

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (child != null && (child.name == VisualCellName || child.name == BackgroundName))
                {
                    DestroySafe(child.gameObject);
                }
            }
        }

        private void EnsureBackground(Transform root, float step, float xOffset, float yOffset)
        {
            backgroundRenderer = null;
            if (!backgroundEnabled || root == null || visualConfig == null)
            {
                return;
            }

            GameObject backgroundObject = new GameObject(BackgroundName);
            backgroundObject.transform.SetParent(root, false);
            backgroundObject.transform.localPosition = new Vector3(0f, 0f, 0.1f);
            backgroundObject.transform.localRotation = Quaternion.identity;

            backgroundRenderer = backgroundObject.AddComponent<SpriteRenderer>();
            backgroundRenderer.sprite = GetSolidSquareSprite();
            backgroundRenderer.drawMode = SpriteDrawMode.Simple;
            backgroundRenderer.color = backgroundColor;
            backgroundRenderer.sortingOrder = sortingOrderBase - 10;

            float width = (visualConfig.Width * visualConfig.CellSize) + ((visualConfig.Width - 1) * spacing) + (backgroundPadding * 2f);
            float height = (visualConfig.Height * visualConfig.CellSize) + ((visualConfig.Height - 1) * spacing) + (backgroundPadding * 2f);
            backgroundObject.transform.localScale = GetCompensatedSizeScale(backgroundObject.transform.parent, width, height);
        }

        private void HideState(VisualCellState state)
        {
            if (state == null || state.Renderer == null || !state.IsVisible)
            {
                return;
            }

            state.IsVisible = false;
            StopHideCoroutine(state);
            if (!hideAnimationEnabled || !Application.isPlaying)
            {
                state.Renderer.enabled = false;
                return;
            }

            state.HideCoroutine = StartCoroutine(PlayHideAnimation(state));
        }

        private IEnumerator PlayHideAnimation(VisualCellState state)
        {
            if (state == null || state.Renderer == null)
            {
                yield break;
            }

            SpriteRenderer renderer = state.Renderer;
            Color startColor = state.OriginalColor;
            Vector3 startScale = state.OriginalScale;
            Vector3 endScale = startScale * hideAnimationEndScaleMultiplier;
            float duration = Mathf.Max(0.01f, hideAnimationDuration);
            float elapsed = 0f;

            renderer.enabled = true;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                Color animatedColor = startColor;
                animatedColor.a = Mathf.Lerp(startColor.a, 0f, t);
                renderer.color = animatedColor;
                renderer.transform.localScale = Vector3.Lerp(startScale, endScale, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            renderer.enabled = false;
            renderer.color = state.OriginalColor;
            renderer.transform.localScale = state.OriginalScale;
            state.HideCoroutine = null;
        }

        private void StopHideCoroutine(VisualCellState state)
        {
            if (state == null || state.HideCoroutine == null)
            {
                return;
            }

            StopCoroutine(state.HideCoroutine);
            state.HideCoroutine = null;
        }

        private int ConvertDataYToRenderRow(int dataY)
        {
            // Existing generated LargePatternVisualConfig assets store Texture2D rows in Unity's
            // bottom-to-top texture coordinate order. The visual grid renders rows top-to-bottom,
            // so convert data Y into the render row here instead of requiring assets to regenerate.
            return visualConfig.Height - 1 - dataY;
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
            return GetCompensatedSizeScale(parent, cellSize, cellSize);
        }

        private static Vector3 GetCompensatedSizeScale(Transform parent, float width, float height)
        {
            Vector3 parentLossyScale = parent != null ? parent.lossyScale : Vector3.one;
            float scaleX = SafeDiv(width, parentLossyScale.x);
            float scaleY = SafeDiv(height, parentLossyScale.y);
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
                int yCompare = right.RenderY.CompareTo(left.RenderY);
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

            int yFallback = left.RenderY.CompareTo(right.RenderY);
            return yFallback != 0 ? yFallback : left.RenderX.CompareTo(right.RenderX);
        }

        private void ClampRegionToVisualConfig(ref int startX, ref int endX, ref int startY, ref int endY)
        {
            int minX = Mathf.Min(startX, endX);
            int maxX = Mathf.Max(startX, endX);
            int minY = Mathf.Min(startY, endY);
            int maxY = Mathf.Max(startY, endY);

            startX = Mathf.Clamp(minX, 0, visualConfig.Width - 1);
            endX = Mathf.Clamp(maxX, 0, visualConfig.Width - 1);
            startY = Mathf.Clamp(minY, 0, visualConfig.Height - 1);
            endY = Mathf.Clamp(maxY, 0, visualConfig.Height - 1);
        }

        private static int GetDeterministicCellHash(VisualCellState state, int seed)
        {
            unchecked
            {
                int hash = seed;
                hash = (hash * 397) ^ state.DataX;
                hash = (hash * 397) ^ state.DataY;
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
