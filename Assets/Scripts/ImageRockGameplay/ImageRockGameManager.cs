using UnityEngine;

namespace EliminateGame.ImageRockGameplay
{
    public sealed class ImageRockGameManager : MonoBehaviour
    {
        [SerializeField] private ImageRockGridController rockGrid;
        [SerializeField] private ImageRockSelectionAreaController selectionArea;
        [SerializeField] private int rocksRemovedPerClick = 3;
        [SerializeField] private GameObject winPanel;

        private bool hasWon;

        private void Start()
        {
            if (winPanel != null) winPanel.SetActive(false);
            rockGrid.BuildGrid();
            selectionArea.RebuildFromBottomExposedCounts(rockGrid.GetBottomExposedColorCounts());
            selectionArea.TileClicked += OnTileClicked;
        }

        private void OnDestroy()
        {
            if (selectionArea != null) selectionArea.TileClicked -= OnTileClicked;
        }

        private void OnTileClicked(ImageRockSelectionTile tile)
        {
            if (hasWon || tile == null) return;
            int removed = rockGrid.RemoveBottomExposedRocks(tile.Color, rocksRemovedPerClick);
            if (removed <= 0)
            {
                Debug.LogWarning($"No bottom-exposed ImageRock rocks available for {tile.Color}; tile was not consumed.");
                return;
            }
            selectionArea.ConsumeTile(tile);
            rockGrid.ApplyColumnGravity();
            if (rockGrid.GetRemainingCount() <= 0)
            {
                hasWon = true;
                if (winPanel != null) winPanel.SetActive(true);
                selectionArea.ClearTiles();
                return;
            }
            selectionArea.RebuildFromBottomExposedCounts(rockGrid.GetBottomExposedColorCounts());
        }
    }
}
