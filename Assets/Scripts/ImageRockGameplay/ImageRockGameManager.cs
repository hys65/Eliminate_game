using UnityEngine;

namespace EliminateGame.ImageRockGameplay
{
    public sealed class ImageRockGameManager : MonoBehaviour
    {
        [SerializeField] private ImageRockGridController rockGrid;
        [SerializeField] private ImageRockSelectionAreaController selectionArea;
        [SerializeField] private int rocksRemovedPerClick = 3;
        [SerializeField] private GameObject winPanel;
        [SerializeField] private bool showRestartButton = true;

        private bool hasWon;

        private void Start()
        {
            selectionArea.TileClicked += OnTileClicked;
            RestartRun();
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
        }

        public void RestartRun()
        {
            hasWon = false;
            if (winPanel != null) winPanel.SetActive(false);
            rockGrid.ClearGrid();
            selectionArea.ClearTiles();
            rockGrid.BuildGrid();
            selectionArea.BuildFixedPoolFromTotalCounts(rockGrid.GetInitialColorCounts());
        }

        private void OnGUI()
        {
            if (!showRestartButton) return;
            if (GUI.Button(new Rect(16, 16, 96, 32), "Restart"))
            {
                RestartRun();
            }
        }
    }
}
