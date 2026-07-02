using System;
using System.Collections.Generic;
using UnityEngine;

namespace EliminateGame.ImageRockGameplay
{
    public sealed class ImageRockSelectionAreaController : MonoBehaviour
    {
        [SerializeField] private Transform tileRoot;
        [SerializeField] private float tileSize = 0.30f;
        [SerializeField] private float spacing = 0.36f;
        [SerializeField] private int columns = 12;
        [SerializeField] private int sortingOrderBase = 200;

        public event Action<ImageRockSelectionTile> TileClicked;

        private readonly List<ImageRockSelectionTile> tiles = new List<ImageRockSelectionTile>();
        private static readonly ImageRockColor[] ColorOrder = { ImageRockColor.Brown, ImageRockColor.Dark, ImageRockColor.Green, ImageRockColor.Cream, ImageRockColor.Pink, ImageRockColor.Yellow, ImageRockColor.White, ImageRockColor.Blue };


        public void BuildFixedPoolFromTotalCounts(Dictionary<ImageRockColor, int> totalCounts)
        {
            ClearTiles();
            if (tileRoot == null) tileRoot = transform;
            int index = 0;
            foreach (ImageRockColor color in ColorOrder)
            {
                int totalCount = totalCounts != null && totalCounts.ContainsKey(color) ? totalCounts[color] : 0;
                int tileCount = totalCount > 0 ? Mathf.CeilToInt(totalCount / 3f) : 0;
                for (int i = 0; i < tileCount; i++) CreateTile(color, index++);
            }
        }

        public void RebuildFromBottomExposedCounts(Dictionary<ImageRockColor, int> bottomCounts)
        {
            ClearTiles();
            if (tileRoot == null) tileRoot = transform;
            int index = 0;
            foreach (ImageRockColor color in ColorOrder)
            {
                int bottomCount = bottomCounts != null && bottomCounts.ContainsKey(color) ? bottomCounts[color] : 0;
                int tileCount = bottomCount > 0 ? Mathf.CeilToInt(bottomCount / 3f) : 0;
                for (int i = 0; i < tileCount; i++) CreateTile(color, index++);
            }
        }

        public void ClearTiles()
        {
            if (tileRoot == null) tileRoot = transform;
            for (int i = tileRoot.childCount - 1; i >= 0; i--) DestroyChild(tileRoot.GetChild(i).gameObject);
            tiles.Clear();
        }

        public void ConsumeTile(ImageRockSelectionTile tile)
        {
            if (tile == null || tile.IsRemoved) return;
            tile.Remove();
            tiles.Remove(tile);
        }

        private void CreateTile(ImageRockColor color, int index)
        {
            GameObject go = new GameObject($"ImageRockSelectionTile_{color}_{index:00}");
            go.transform.SetParent(tileRoot, false);
            int col = index % columns;
            int row = index / columns;
            go.transform.localPosition = new Vector3((col - (columns - 1) * 0.5f) * spacing, -row * spacing, 0f);
            go.transform.localScale = Vector3.one * tileSize;
            go.AddComponent<SpriteRenderer>();
            go.AddComponent<BoxCollider2D>();
            ImageRockSelectionTile tile = go.AddComponent<ImageRockSelectionTile>();
            tile.Initialize(color, ImageRockGridController.GetGeneratedSprite(), ImageRockGridController.ToDisplayColor(color), sortingOrderBase + row);
            tile.Clicked += OnTileClicked;
            tiles.Add(tile);
        }

        private void OnTileClicked(ImageRockSelectionTile tile)
        {
            TileClicked?.Invoke(tile);
        }
        private static void DestroyChild(GameObject child)
        {
            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }

    }
}
