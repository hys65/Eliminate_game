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
        [SerializeField] private float spacing = 1.0f;

        private readonly Dictionary<Vector2Int, SelectionTile> tiles = new Dictionary<Vector2Int, SelectionTile>();

        public event Action<SelectionTile> TileSelected;

        public void Initialize(GameConfig config)
        {
            ClearExistingTiles();

            var validDefinitions = new List<GameConfig.SelectionTileDefinition>();
            var seenPositions = new HashSet<Vector2Int>();

            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;

            foreach (GameConfig.SelectionTileDefinition definition in config.SelectionTiles)
            {
                if (definition.Color == BlockColor.None)
                {
                    continue;
                }

                Vector2Int key = new Vector2Int(definition.X, definition.Y);
                if (!seenPositions.Add(key))
                {
                    Debug.LogWarning($"Duplicate Selection Area tile definition at {key}. Skipping duplicate.");
                    continue;
                }

                validDefinitions.Add(definition);

                if (definition.X < minX) minX = definition.X;
                if (definition.X > maxX) maxX = definition.X;
                if (definition.Y < minY) minY = definition.Y;
                if (definition.Y > maxY) maxY = definition.Y;
            }

            Vector3 centerOffset = Vector3.zero;
            if (validDefinitions.Count > 0)
            {
                float centerX = (minX + maxX) * 0.5f * spacing;
                float centerY = -(minY + maxY) * 0.5f * spacing;
                centerOffset = new Vector3(centerX, centerY, 0f);
            }

            Transform parent = tileRoot != null ? tileRoot : transform;

            foreach (GameConfig.SelectionTileDefinition definition in validDefinitions)
            {
                Vector2Int key = new Vector2Int(definition.X, definition.Y);

                var tile = Instantiate(tilePrefab, parent);
                tile.transform.localPosition = new Vector3(definition.X * spacing, -definition.Y * spacing, 0f) - centerOffset;
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
            Debug.Log($"HandleTileClicked: ({tile.X},{tile.Y}) {tile.Color}");
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
