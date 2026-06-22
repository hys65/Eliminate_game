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
            FullMappedRegion,
            PartialMappedRegion
        }
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

        [Header("Visual-only removal pacing")]
        [SerializeField] private LargeVisualRemovalMode removalMode = LargeVisualRemovalMode.PartialMappedRegion;
        [SerializeField, Range(0.1f, 1f)] private float partialHideRatio = 0.45f;
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

            GetMappedRegion(removedCell, out int startX, out int endX, out int startY, out int endY);

            if (removalMode == LargeVisualRemovalMode.FullMappedRegion)
            {
                AddFullMappedRegion(startX, endX, startY, endY, coordinatesToHide);
                return;
            }

            AddPartialMappedRegion(startX, endX, startY, endY, coordinatesToHide);
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

        private static void AddFullMappedRegion(int startX, int endX, int startY, int endY, HashSet<Vector2Int> coordinatesToHide)
        {
            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    coordinatesToHide.Add(new Vector2Int(x, y));
                }
            }
        }

        private void AddPartialMappedRegion(int startX, int endX, int startY, int endY, HashSet<Vector2Int> coordinatesToHide)
        {
            List<Vector2Int> visibleCoordinates = new List<Vector2Int>();
            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    if (largeVisualController != null && largeVisualController.IsCellVisible(x, y))
                    {
                        visibleCoordinates.Add(new Vector2Int(x, y));
                    }
                }
            }

            if (visibleCoordinates.Count <= 0)
            {
                return;
            }

            visibleCoordinates.Sort(CompareDeterministicHideOrder);
            int hideCount = Mathf.Clamp(Mathf.CeilToInt(visibleCoordinates.Count * partialHideRatio), 1, visibleCoordinates.Count);
            for (int i = 0; i < hideCount; i++)
            {
                coordinatesToHide.Add(visibleCoordinates[i]);
            }
        }

        private int CompareDeterministicHideOrder(Vector2Int left, Vector2Int right)
        {
            int leftHash = GetDeterministicCellHash(left);
            int rightHash = GetDeterministicCellHash(right);
            int hashCompare = leftHash.CompareTo(rightHash);
            if (hashCompare != 0)
            {
                return hashCompare;
            }

            int yCompare = left.y.CompareTo(right.y);
            return yCompare != 0 ? yCompare : left.x.CompareTo(right.x);
        }

        private int GetDeterministicCellHash(Vector2Int coordinate)
        {
            unchecked
            {
                int hash = deterministicHideSeed;
                hash = (hash * 397) ^ coordinate.x;
                hash = (hash * 397) ^ coordinate.y;
                return hash & int.MaxValue;
            }
        }
    }
}
