using System;
using System.Collections.Generic;
using EliminateGame.Data;
using EliminateGame.Pattern;
using UnityEngine;

namespace EliminateGame.SelectionArea
{
    public class SelectionAreaGridController : MonoBehaviour
    {
        [SerializeField] private SelectionTile tilePrefab;
        [SerializeField] private Transform tileRoot;
        [SerializeField] private float spacing = 1.1f;

        private readonly Dictionary<Vector2Int, SelectionTile> tiles = new Dictionary<Vector2Int, SelectionTile>();

        public event Action<SelectionTile> TileSelected;

        public void Initialize(GameConfig config)
        {
            ClearExistingTiles();

            foreach (GameConfig.SelectionTileDefinition definition in config.SelectionTiles)
            {
                if (definition.Color == BlockColor.None)
                {
                    continue;
                }

                Vector2Int key = new Vector2Int(definition.X, definition.Y);
                if (tiles.ContainsKey(key))
                {
                    Debug.LogWarning($"Duplicate Selection Area tile definition at {key}. Skipping duplicate.");
                    continue;
                }

                var tile = Instantiate(tilePrefab, tileRoot != null ? tileRoot : transform);
                tile.transform.localPosition = new Vector3(definition.X * spacing, definition.Y * spacing, 0f);
                tile.Initialize(definition.X, definition.Y, definition.Color, definition.StartUnlocked);
                tile.Clicked += HandleTileClicked;
                tiles.Add(key, tile);
            }

            Debug.Log($"Selection Area initialized. TileCount={tiles.Count}");
        }

        public int RemainingTileCount()
        {
            int count = 0;
            foreach (SelectionTile tile in tiles.Values)
            {
                if (!tile.IsRemoved)
                {
                    count++;
                }
            }

            return count;
        }

        private void HandleTileClicked(SelectionTile tile)
        {
            TileSelected?.Invoke(tile);
        }

        public void ConsumeTileAndUnlockCrossNeighbors(SelectionTile tile)
        {
            tile.RemoveFromSelectionArea();
            UnlockIfExists(tile.X + 1, tile.Y);
            UnlockIfExists(tile.X - 1, tile.Y);
            UnlockIfExists(tile.X, tile.Y + 1);
            UnlockIfExists(tile.X, tile.Y - 1);
        }

        private void UnlockIfExists(int x, int y)
        {
            Vector2Int key = new Vector2Int(x, y);
            if (!tiles.TryGetValue(key, out SelectionTile tile))
            {
                return;
            }

            if (tile.IsRemoved || tile.IsUnlocked)
            {
                return;
            }

            tile.SetUnlocked(true);
        }

        private void ClearExistingTiles()
        {
            foreach (SelectionTile tile in tiles.Values)
            {
                if (tile != null)
                {
                    tile.Clicked -= HandleTileClicked;
                    Destroy(tile.gameObject);
                }
            }

            tiles.Clear();
        }
    }
}
