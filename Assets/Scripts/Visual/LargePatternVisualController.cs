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
        private readonly List<SpriteRenderer> visualCells = new List<SpriteRenderer>();

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
                    BlockColor color = visualConfig.GetCell(x, y);
                    if (color == BlockColor.None)
                    {
                        continue;
                    }

                    nonNoneCellCount++;
                    SpriteRenderer renderer = CreateVisualCell(root, color);
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

                    visualCells.Add(renderer);
                }
            }

            Debug.Log($"[LargePatternVisual] Built visual grid Width={visualConfig.Width} Height={visualConfig.Height} NonNoneCells={nonNoneCellCount}", this);
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

        private SpriteRenderer CreateVisualCell(Transform root, BlockColor color)
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
            renderer.color = MapColor(color);
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
