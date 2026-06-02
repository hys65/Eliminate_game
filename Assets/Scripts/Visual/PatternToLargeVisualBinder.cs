using System.Collections.Generic;
using EliminateGame.Core;
using EliminateGame.Pattern;
using UnityEngine;

namespace EliminateGame.Visual
{
    public class PatternToLargeVisualBinder : MonoBehaviour
    {
        [Header("Controllers")]
        [SerializeField] private PatternController patternController;
        [SerializeField] private LargePatternVisualController largeVisualController;

        [Header("Gameplay Pattern Dimensions")]
        [SerializeField, Min(1)] private int gameplayPatternWidth = 6;
        [SerializeField, Min(1)] private int gameplayPatternHeight = 6;

        [Header("Large Visual Dimensions")]
        [SerializeField, Min(1)] private int visualWidth = 30;
        [SerializeField, Min(1)] private int visualHeight = 28;

        [Header("Binding")]
        [SerializeField] private bool bindOnStart = true;

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
            isSubscribedToGameManager = true;
        }

        private void OnRunInitialized()
        {
            largeVisualController?.ResetVisualState();
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

            HashSet<Vector2Int> coordinatesToHide = new HashSet<Vector2Int>();
            for (int i = 0; i < removedCells.Count; i++)
            {
                AddMappedCoordinates(removedCells[i], coordinatesToHide);
            }

            largeVisualController.HideCells(coordinatesToHide);
        }

        private void AddMappedCoordinates(PatternRemovedCell removedCell, HashSet<Vector2Int> coordinatesToHide)
        {
            if (coordinatesToHide == null)
            {
                return;
            }

            int startX = Mathf.FloorToInt(removedCell.Column * visualWidth / (float)gameplayPatternWidth);
            int endX = Mathf.FloorToInt((removedCell.Column + 1) * visualWidth / (float)gameplayPatternWidth) - 1;
            int startY = Mathf.FloorToInt(removedCell.Row * visualHeight / (float)gameplayPatternHeight);
            int endY = Mathf.FloorToInt((removedCell.Row + 1) * visualHeight / (float)gameplayPatternHeight) - 1;

            startX = Mathf.Clamp(startX, 0, visualWidth - 1);
            endX = Mathf.Clamp(endX, 0, visualWidth - 1);
            startY = Mathf.Clamp(startY, 0, visualHeight - 1);
            endY = Mathf.Clamp(endY, 0, visualHeight - 1);

            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    coordinatesToHide.Add(new Vector2Int(x, y));
                }
            }
        }
    }
}
