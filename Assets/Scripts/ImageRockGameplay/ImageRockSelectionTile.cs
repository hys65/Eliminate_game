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
            Color = color;
            IsRemoved = false;
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>() ?? gameObject.AddComponent<SpriteRenderer>();
            if (boxCollider2D == null) boxCollider2D = GetComponent<BoxCollider2D>() ?? gameObject.AddComponent<BoxCollider2D>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = displayColor;
            spriteRenderer.sortingOrder = sortingOrder;
            spriteRenderer.enabled = true;
            boxCollider2D.enabled = true;
            boxCollider2D.size = Vector2.one;
        }

        public void SetInteractable(bool interactable)
        {
            if (boxCollider2D != null && !IsRemoved) boxCollider2D.enabled = interactable;
        }

        public void Remove()
        {
            IsRemoved = true;
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            if (boxCollider2D != null) boxCollider2D.enabled = false;
        }

        private void OnMouseDown()
        {
            if (IsRemoved || boxCollider2D == null || !boxCollider2D.enabled) return;
            Clicked?.Invoke(this);
        }
    }
}
