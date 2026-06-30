using System.Collections.Generic;
using EliminateGame.Core;
using EliminateGame.Pattern;
using UnityEngine;

namespace EliminateGame.Visual
{
    public class PatternToLargeVisualBinder : MonoBehaviour
    {
        private enum LargeVisualRemovalMode
        {
            Region,
            PaletteTarget,
            LocalRegionPalette
        }

        [Header("Controllers")]
        [SerializeField] private PatternController patternController;
        [SerializeField] private LargePatternVisualController largeVisualController;
        [SerializeField] private GameplayColorVisualMapping visualMapping;

        [Header("Gameplay Pattern Dimensions")]
        [SerializeField, Min(1)] private int gameplayPatternWidth = 6;
        [SerializeField, Min(1)] private int gameplayPatternHeight = 6;

        [Header("Large Visual Dimensions")]
        [SerializeField, Min(1)] private int visualWidth = 30;
        [SerializeField, Min(1)] private int visualHeight = 28;

        [Header("Binding")]
        [SerializeField] private bool bindOnStart = true;

        [Header("Visual-only removal pacing")]
        [SerializeField] private LargeVisualRemovalMode removalMode = LargeVisualRemovalMode.LocalRegionPalette;
        [SerializeField, Min(1)] private int cellsToHidePerGameplayCell = 3;
        [SerializeField] private bool preferBottomToTop = false;
        [SerializeField] private int deterministicHideSeed = 12345;

        private GameManager gameManager;
        private bool isSubscribedToPattern;
        private bool isSubscribedToGameManager;

        private void Start()
        {
            if (bindOnStart)
            {
                Bind();
            }
        }

        private void OnDestroy()
        {
            Unbind();
        }

        public void Bind()
        {
            SubscribeToPatternController();
            SubscribeToGameManager();
            largeVisualController?.ResetVisualState();
        }

        public void Unbind()
        {
            if (isSubscribedToPattern && patternController != null)
            {
                patternController.CellsRemoved -= OnPatternCellsRemoved;
            }

            if (isSubscribedToGameManager && gameManager != null)
            {
                gameManager.RunInitialized -= OnRunInitialized;
                gameManager.RunWon -= OnRunWon;
            }

            isSubscribedToPattern = false;
            isSubscribedToGameManager = false;
        }

        private void SubscribeToPatternController()
        {
            if (isSubscribedToPattern || patternController == null)
            {
                return;
            }

            patternController.CellsRemoved += OnPatternCellsRemoved;
            isSubscribedToPattern = true;
        }

        private void SubscribeToGameManager()
        {
            if (isSubscribedToGameManager)
            {
                return;
            }

            gameManager = FindObjectOfType<GameManager>();
            if (gameManager == null)
            {
                return;
            }

            gameManager.RunInitialized += OnRunInitialized;
            gameManager.RunWon += OnRunWon;
            isSubscribedToGameManager = true;
        }

        private void OnRunInitialized()
        {
            largeVisualController?.ResetVisualState();
        }

        private void OnRunWon()
        {
            largeVisualController?.HideAllCells();
        }

        private void OnPatternCellsRemoved(IReadOnlyList<PatternRemovedCell> removedCells)
        {
            if (largeVisualController == null || removedCells == null || removedCells.Count <= 0)
            {
                return;
            }

            if (gameplayPatternWidth <= 0 || gameplayPatternHeight <= 0 || visualWidth <= 0 || visualHeight <= 0)
            {
                return;
            }

            if (removalMode == LargeVisualRemovalMode.PaletteTarget)
            {
                HidePaletteTargetsOrFallback(removedCells);
                return;
            }

            if (removalMode == LargeVisualRemovalMode.LocalRegionPalette)
            {
                HideLocalRegionPaletteTargetsOrFallback(removedCells);
                return;
            }

            HideMappedRegions(removedCells);
        }

        private void HideLocalRegionPaletteTargetsOrFallback(IReadOnlyList<PatternRemovedCell> removedCells)
        {
            for (int i = 0; i < removedCells.Count; i++)
            {
                PatternRemovedCell removedCell = removedCells[i];
                GetMappedRegion(removedCell, out int startX, out int endX, out int startY, out int endY);

                int hiddenCount = 0;
                if (visualMapping != null && visualMapping.TryGetTargetPaletteIndices(removedCell.Color, out IReadOnlyList<int> targetPaletteIndices))
                {
                    hiddenCount = largeVisualController.HideCellsByPaletteIndicesInRegion(
                        targetPaletteIndices,
                        cellsToHidePerGameplayCell,
                        deterministicHideSeed + i,
                        startX,
                        endX,
                        startY,
                        endY,
                        preferBottomToTop);
                }

                if (hiddenCount <= 0)
                {
                    hiddenCount = largeVisualController.HideAnyVisibleCellsInRegion(
                        cellsToHidePerGameplayCell,
                        deterministicHideSeed + i,
                        startX,
                        endX,
                        startY,
                        endY,
                        preferBottomToTop);
                }

                if (hiddenCount <= 0)
                {
                    HideMappedRegion(removedCell);
                }
            }
        }

        private void HidePaletteTargetsOrFallback(IReadOnlyList<PatternRemovedCell> removedCells)
        {
            for (int i = 0; i < removedCells.Count; i++)
            {
                PatternRemovedCell removedCell = removedCells[i];
                int hiddenCount = 0;
                if (visualMapping != null && visualMapping.TryGetTargetPaletteIndices(removedCell.Color, out IReadOnlyList<int> targetPaletteIndices))
                {
                    hiddenCount = largeVisualController.HideCellsByPaletteIndices(targetPaletteIndices, cellsToHidePerGameplayCell, deterministicHideSeed + i, preferBottomToTop);
                }

                if (hiddenCount <= 0)
                {
                    hiddenCount = largeVisualController.HideAnyVisibleCells(cellsToHidePerGameplayCell, deterministicHideSeed + i, preferBottomToTop);
                }

                if (hiddenCount <= 0)
                {
                    HideMappedRegion(removedCell);
                }
            }
        }

        private void HideMappedRegions(IReadOnlyList<PatternRemovedCell> removedCells)
        {
            HashSet<Vector2Int> coordinatesToHide = new HashSet<Vector2Int>();
            for (int i = 0; i < removedCells.Count; i++)
            {
                AddMappedRegion(removedCells[i], coordinatesToHide);
            }

            largeVisualController.HideCells(coordinatesToHide);
        }

        private void HideMappedRegion(PatternRemovedCell removedCell)
        {
            HashSet<Vector2Int> coordinatesToHide = new HashSet<Vector2Int>();
            AddMappedRegion(removedCell, coordinatesToHide);
            largeVisualController.HideCells(coordinatesToHide);
        }

        private void AddMappedRegion(PatternRemovedCell removedCell, HashSet<Vector2Int> coordinatesToHide)
        {
            if (coordinatesToHide == null)
            {
                return;
            }

            GetMappedRegion(removedCell, out int startX, out int endX, out int startY, out int endY);
            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    coordinatesToHide.Add(new Vector2Int(x, y));
                }
            }
        }

        private void GetMappedRegion(PatternRemovedCell removedCell, out int startX, out int endX, out int startY, out int endY)
        {
            int originalColumn = removedCell.OriginalColumn;
            int originalRow = removedCell.OriginalRow;

            startX = Mathf.FloorToInt(originalColumn * visualWidth / (float)gameplayPatternWidth);
            endX = Mathf.FloorToInt((originalColumn + 1) * visualWidth / (float)gameplayPatternWidth) - 1;
            startY = Mathf.FloorToInt(originalRow * visualHeight / (float)gameplayPatternHeight);
            endY = Mathf.FloorToInt((originalRow + 1) * visualHeight / (float)gameplayPatternHeight) - 1;

            startX = Mathf.Clamp(startX, 0, visualWidth - 1);
            endX = Mathf.Clamp(endX, 0, visualWidth - 1);
            startY = Mathf.Clamp(startY, 0, visualHeight - 1);
            endY = Mathf.Clamp(endY, 0, visualHeight - 1);
        }
    }
}
