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
        [SerializeField, Range(0.1f, 1f)] private float lockedBrightnessMultiplier = 0.4f;
        [SerializeField] private int sortingOrderBase = 200;

        private static Sprite generatedTileSprite;
        private SpriteRenderer spriteRenderer;
        private BoxCollider2D boxCollider2D;

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
            transform.localScale = new Vector3(0.6f, 0.6f, 1f);

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
            spriteRenderer.sortingOrder = sortingOrderBase;

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

            var fallbackColor = ToUnityColor(color);
            var baseColor = visualMapping != null ? visualMapping.GetDisplayColor(color, fallbackColor) : fallbackColor;
            var displayColor = isUnlocked ? baseColor : ApplyLockedBrightness(baseColor);
            spriteRenderer.enabled = !IsRemoved;
            spriteRenderer.color = displayColor;
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
