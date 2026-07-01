using System;
using EliminateGame.Pattern;
using EliminateGame.Audio;
using EliminateGame.Visual;
using UnityEngine;

namespace EliminateGame.SelectionArea
{
    public class SelectionTile : MonoBehaviour
    {
        [SerializeField] private BlockColor color;
        [SerializeField] private int x;
        [SerializeField] private int y;
        [SerializeField] private bool isUnlocked;
        [SerializeField] private GameplayColorVisualMapping visualMapping;
        [SerializeField, Min(1)] private int visualSubCellCount = 3;
        [SerializeField, Min(0.05f)] private float visualSubCellSize = 0.28f;
        [SerializeField, Min(0f)] private float visualSubCellSpacing = 0.04f;
        [SerializeField, Range(0.1f, 1f)] private float lockedBrightnessMultiplier = 0.4f;
        [SerializeField] private int sortingOrderBase = 200;

        private static Sprite generatedTileSprite;
        private SpriteRenderer spriteRenderer;
        private BoxCollider2D boxCollider2D;
        private SpriteRenderer[] visualSubCellRenderers;

        public BlockColor Color => color;
        public int X => x;
        public int Y => y;
        public bool IsUnlocked => isUnlocked;
        public bool IsRemoved { get; private set; }

        public event Action<SelectionTile> Clicked;

        private void Awake()
        {
            EnsureComponents();
            UpdateVisualState();
        }

        public void Initialize(int gridX, int gridY, BlockColor tileColor, bool startUnlocked, GameplayColorVisualMapping mapping = null)
        {
            EnsureComponents();

            x = gridX;
            y = gridY;
            color = tileColor;
            visualMapping = mapping;
            IsRemoved = false;
            transform.localScale = new Vector3(0.95f, 0.95f, 1f);

            SetUnlocked(startUnlocked);
            name = $"SelectionTile_{x}_{y}_{color}";
            UpdateVisualState();
        }

        public void SetUnlocked(bool unlocked)
        {
            if (IsRemoved)
            {
                return;
            }

            isUnlocked = unlocked;
            UpdateVisualState();
            Debug.Log($"Selection Area tile ({x},{y}) {color} unlocked={isUnlocked}");
        }

        public void RemoveFromSelectionArea()
        {
            if (IsRemoved)
            {
                return;
            }

            IsRemoved = true;
            isUnlocked = false;
            gameObject.SetActive(false);
            Debug.Log($"Selection Area tile ({x},{y}) {color} removed.");
        }

        private void OnMouseDown()
        {
            Debug.Log($"OnMouseDown hit: ({x},{y}) {color}, enabled={enabled}, removed={IsRemoved}, unlocked={isUnlocked}");

            if (!enabled || IsRemoved || !isUnlocked)
            {
                Debug.Log($"Click ignored: ({x},{y}) {color}");
                return;
            }

            Debug.Log($"Click accepted: ({x},{y}) {color}");
            SfxController.Instance?.PlaySelectionClick();
            Clicked?.Invoke(this);
        }

        private void EnsureComponents()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                {
                    spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                }
            }

            if (generatedTileSprite == null)
            {
                var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "SelectionTileGeneratedTexture"
                };
                texture.SetPixel(0, 0, UnityEngine.Color.white);
                texture.Apply();

                generatedTileSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f),
                    1f);
                generatedTileSprite.name = "SelectionTileGeneratedSprite";
            }

            spriteRenderer.sprite = generatedTileSprite;
            spriteRenderer.enabled = false;

            EnsureVisualSubCells();

            if (boxCollider2D == null)
            {
                boxCollider2D = GetComponent<BoxCollider2D>();
                if (boxCollider2D == null)
                {
                    boxCollider2D = gameObject.AddComponent<BoxCollider2D>();
                }
            }

            boxCollider2D.size = new Vector2(1f, 1f);
        }

        private void UpdateVisualState()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.enabled = false;
            EnsureVisualSubCells();

            var fallbackColor = ToUnityColor(color);
            var baseColor = visualMapping != null ? visualMapping.GetDisplayColor(color, fallbackColor) : fallbackColor;
            var displayColor = isUnlocked ? baseColor : ApplyLockedBrightness(baseColor);
            bool shouldShow = !IsRemoved;

            for (int i = 0; i < visualSubCellRenderers.Length; i++)
            {
                SpriteRenderer subCellRenderer = visualSubCellRenderers[i];
                if (subCellRenderer == null)
                {
                    continue;
                }

                subCellRenderer.enabled = shouldShow;
                subCellRenderer.color = displayColor;
            }
        }

        private void EnsureVisualSubCells()
        {
            int safeCount = Mathf.Max(1, visualSubCellCount);
            if (visualSubCellRenderers == null || visualSubCellRenderers.Length != safeCount)
            {
                visualSubCellRenderers = new SpriteRenderer[safeCount];
            }

            float step = visualSubCellSize + visualSubCellSpacing;
            float startX = -((safeCount - 1) * step * 0.5f);

            for (int i = 0; i < safeCount; i++)
            {
                SpriteRenderer subCellRenderer = visualSubCellRenderers[i];
                if (subCellRenderer == null)
                {
                    Transform existing = transform.Find($"SelectionTileVisualSubCell_{i}");
                    GameObject subCellObject = existing != null
                        ? existing.gameObject
                        : new GameObject($"SelectionTileVisualSubCell_{i}");

                    subCellObject.transform.SetParent(transform, false);
                    subCellRenderer = subCellObject.GetComponent<SpriteRenderer>();
                    if (subCellRenderer == null)
                    {
                        subCellRenderer = subCellObject.AddComponent<SpriteRenderer>();
                    }

                    visualSubCellRenderers[i] = subCellRenderer;
                }

                subCellRenderer.name = $"SelectionTileVisualSubCell_{i}";
                subCellRenderer.sprite = generatedTileSprite;
                subCellRenderer.sortingOrder = sortingOrderBase + i;
                subCellRenderer.transform.localPosition = new Vector3(startX + (i * step), 0f, 0f);
                subCellRenderer.transform.localScale = new Vector3(visualSubCellSize, visualSubCellSize, 1f);
            }
        }

        private UnityEngine.Color ApplyLockedBrightness(UnityEngine.Color source)
        {
            return new UnityEngine.Color(
                source.r * lockedBrightnessMultiplier,
                source.g * lockedBrightnessMultiplier,
                source.b * lockedBrightnessMultiplier,
                source.a);
        }

        private static UnityEngine.Color ToUnityColor(BlockColor blockColor)
        {
            switch (blockColor)
            {
                case BlockColor.Red:
                    return UnityEngine.Color.red;
                case BlockColor.Blue:
                    return UnityEngine.Color.blue;
                case BlockColor.Green:
                    return UnityEngine.Color.green;
                case BlockColor.Yellow:
                    return UnityEngine.Color.yellow;
                case BlockColor.Purple:
                    return new UnityEngine.Color(0.6f, 0.2f, 0.8f);
                default:
                    return UnityEngine.Color.white;
            }
        }
    }
}
