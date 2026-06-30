using System.Collections.Generic;
using EliminateGame.Pattern;
using UnityEngine;

namespace EliminateGame.Visual
{
    public class CurrentTargetRowPreviewController : MonoBehaviour
    {
        [SerializeField] private PatternController patternController;
        [SerializeField] private GameplayColorVisualMapping visualMapping;
        [SerializeField] private Transform tileRoot;
        [SerializeField] private float spacing = 0.55f;
        [SerializeField] private float tileSize = 0.42f;
        [SerializeField] private int sortingOrder = 700;
        [SerializeField] private bool refreshEveryFrame = true;

        private readonly List<BlockColor> lastColors = new List<BlockColor>();
        private static Sprite cachedSolidSquareSprite;

        private void Awake()
        {
            if (tileRoot == null)
            {
                tileRoot = transform;
            }
        }

        private void Start()
        {
            RefreshPreviewIfNeeded();
        }

        private void Update()
        {
            if (refreshEveryFrame)
            {
                RefreshPreviewIfNeeded();
            }
        }

        private void OnDestroy()
        {
            ClearPreview();
        }

        private void RefreshPreviewIfNeeded()
        {
            if (patternController == null)
            {
                if (lastColors.Count > 0)
                {
                    lastColors.Clear();
                    ClearPreview();
                }

                return;
            }

            IReadOnlyList<BlockColor> currentColors = patternController.GetBottomRowColors();
            if (!HasColorSequenceChanged(currentColors))
            {
                return;
            }

            lastColors.Clear();
            if (currentColors != null)
            {
                for (int i = 0; i < currentColors.Count; i++)
                {
                    lastColors.Add(currentColors[i]);
                }
            }

            BuildPreview(lastColors);
        }

        private bool HasColorSequenceChanged(IReadOnlyList<BlockColor> colors)
        {
            int currentCount = colors?.Count ?? 0;
            if (lastColors.Count != currentCount)
            {
                return true;
            }

            for (int i = 0; i < currentCount; i++)
            {
                if (lastColors[i] != colors[i])
                {
                    return true;
                }
            }

            return false;
        }

        private void ClearPreview()
        {
            Transform root = tileRoot != null ? tileRoot : transform;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Destroy(root.GetChild(i).gameObject);
            }
        }

        private void BuildPreview(IReadOnlyList<BlockColor> colors)
        {
            ClearPreview();

            if (colors == null || colors.Count <= 0)
            {
                return;
            }

            Transform root = tileRoot != null ? tileRoot : transform;
            float totalWidth = (colors.Count - 1) * spacing;
            float startX = -totalWidth * 0.5f;

            for (int i = 0; i < colors.Count; i++)
            {
                GameObject tile = new GameObject("CurrentTargetRowPreviewTile");
                tile.transform.SetParent(root, false);
                tile.transform.localPosition = new Vector3(startX + i * spacing, 0f, 0f);
                tile.transform.localScale = new Vector3(tileSize, tileSize, 1f);

                SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
                renderer.sprite = GetSolidSquareSprite();
                renderer.drawMode = SpriteDrawMode.Simple;
                renderer.sortingOrder = sortingOrder;
                renderer.color = GetDisplayColor(colors[i]);
            }
        }

        private Color GetDisplayColor(BlockColor color)
        {
            Color fallback = GetFallbackColor(color);
            return visualMapping != null ? visualMapping.GetDisplayColor(color, fallback) : fallback;
        }

        private static Color GetFallbackColor(BlockColor color)
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
                case BlockColor.None:
                default:
                    return Color.clear;
            }
        }

        private static Sprite GetSolidSquareSprite()
        {
            if (cachedSolidSquareSprite != null)
            {
                return cachedSolidSquareSprite;
            }

            Texture2D texture = new Texture2D(1, 1)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            cachedSolidSquareSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            return cachedSolidSquareSprite;
        }
    }
}
