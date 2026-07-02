using UnityEngine;

namespace EliminateGame.ImageRockGameplay
{
    public sealed class ImageRockCell : MonoBehaviour
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public ImageRockColor Color { get; private set; }
        public bool IsRemoved { get; private set; }

        [SerializeField] private SpriteRenderer spriteRenderer;

        public void Initialize(int x, int y, ImageRockColor color, Sprite sprite, Color displayColor, int sortingOrder)
        {
            X = x;
            Y = y;
            Color = color;
            IsRemoved = false;

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = sprite;
            spriteRenderer.color = displayColor;
            spriteRenderer.sortingOrder = sortingOrder;
            spriteRenderer.enabled = true;
        }

        public void SetGridPosition(int x, int y, Vector3 localPosition)
        {
            X = x;
            Y = y;
            transform.localPosition = localPosition;
        }

        public void Remove()
        {
            IsRemoved = true;
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }
        }

        public void Restore()
        {
            IsRemoved = false;
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
            }
        }
    }
}
