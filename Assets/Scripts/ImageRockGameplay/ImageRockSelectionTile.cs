using System;
using UnityEngine;

namespace EliminateGame.ImageRockGameplay
{
    public sealed class ImageRockSelectionTile : MonoBehaviour
    {
        public ImageRockColor Color { get; private set; }
        public bool IsRemoved { get; private set; }

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private BoxCollider2D boxCollider2D;

        public event Action<ImageRockSelectionTile> Clicked;

        public void Initialize(ImageRockColor color, Sprite sprite, Color displayColor, int sortingOrder)
        {
            if (!EnsureComponents())
            {
                enabled = false;
                return;
            }

            Color = color;
            IsRemoved = false;
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = displayColor;
            spriteRenderer.sortingOrder = sortingOrder;
            spriteRenderer.enabled = true;
            boxCollider2D.enabled = true;
            boxCollider2D.size = Vector2.one;
            boxCollider2D.isTrigger = false;
        }

        public void SetInteractable(bool interactable)
        {
            if (!EnsureComponents()) return;
            if (!IsRemoved) boxCollider2D.enabled = interactable;
        }

        public void Remove()
        {
            IsRemoved = true;
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            if (boxCollider2D == null) boxCollider2D = GetComponent<BoxCollider2D>();
            if (boxCollider2D != null) boxCollider2D.enabled = false;
        }

        private void OnMouseDown()
        {
            if (IsRemoved) return;
            if (!EnsureComponents()) return;
            if (!boxCollider2D.enabled) return;
            Clicked?.Invoke(this);
        }

        private bool EnsureComponents()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            if (boxCollider2D == null)
            {
                boxCollider2D = GetComponent<BoxCollider2D>();
            }

            if (boxCollider2D == null)
            {
                boxCollider2D = gameObject.AddComponent<BoxCollider2D>();
            }

            if (spriteRenderer == null || boxCollider2D == null)
            {
                Debug.LogError($"[ImageRockSelectionTile] Missing required components on {name}. SpriteRenderer={spriteRenderer != null}, BoxCollider2D={boxCollider2D != null}", this);
                return false;
            }

            return true;
        }
    }
}
