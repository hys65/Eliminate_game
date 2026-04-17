using System.Collections.Generic;
using System.Linq;
using EliminateGame.Data;
using EliminateGame.Pattern;
using EliminateGame.SelectionArea;
using EliminateGame.TempZone;
using UnityEngine;

namespace EliminateGame.Core
{
    public class GameManager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private GameConfig gameConfig;

        [Header("Controllers")]
        [SerializeField] private PatternController patternController;
        [SerializeField] private TempZoneController tempZoneController;
        [SerializeField] private SelectionAreaGridController selectionAreaGridController;

        [Header("Debug")]
        [SerializeField] private int rescueRandomSeed = 12345;

        private System.Random rescueRandom;
        private int rescueUses;

        public GameState State { get; private set; } = GameState.None;
        public int RescueUses => rescueUses;
        public int RescueUsesLeft => gameConfig != null ? Mathf.Max(0, gameConfig.MaxRescueUses - rescueUses) : 0;

        private void Awake()
        {
            rescueRandom = new System.Random(rescueRandomSeed);
        }

        private void Start()
        {
            StartRun();
        }

        [ContextMenu("Start Run")]
        public void StartRun()
        {
            if (gameConfig == null || patternController == null || tempZoneController == null || selectionAreaGridController == null)
            {
                Debug.LogError("Missing references on GameManager.");
                return;
            }

            rescueUses = 0;
            State = GameState.Running;

            var patternRows = gameConfig.PatternRows
                .Select(row => (IReadOnlyList<BlockColor>)row.Cells)
                .ToList();

            patternController.Initialize(patternRows);
            tempZoneController.Initialize(gameConfig.TempZoneCapacity);

            selectionAreaGridController.TileSelected -= OnSelectionAreaTileSelected;
            selectionAreaGridController.Initialize(gameConfig);
            selectionAreaGridController.TileSelected += OnSelectionAreaTileSelected;

            Debug.Log("Game run started.");
            EvaluateStateAfterAction();
        }

        [ContextMenu("Try Use Rescue")]
        public bool TryUseRescue()
        {
            if (State != GameState.Running)
            {
                Debug.Log("Rescue ignored. Game is not running.");
                return false;
            }

            if (rescueUses >= gameConfig.MaxRescueUses)
            {
                Debug.Log("Rescue failed. Max rescue uses reached.");
                return false;
            }

            if (tempZoneController.Count == 0)
            {
                Debug.Log("Rescue failed. Temp Zone has no tiles.");
                return false;
            }

            var removed = tempZoneController.RemoveRescueTilesWeighted(patternController.GetBottomRowColors(), 3, rescueRandom);
            if (removed.Count <= 0)
            {
                return false;
            }

            rescueUses++;
            Debug.Log($"Rescue used. Count={rescueUses}/{gameConfig.MaxRescueUses}");
            EvaluateStateAfterAction();
            return true;
        }

        private void OnSelectionAreaTileSelected(SelectionTile tile)
        {
            if (State != GameState.Running)
            {
                return;
            }

            if (tempZoneController.IsFull)
            {
                Debug.Log("Selection ignored because Temp Zone is full.");
                EvaluateStateAfterAction();
                return;
            }

            selectionAreaGridController.ConsumeTileAndUnlockCrossNeighbors(tile);
            int tempSlotIndex = tempZoneController.AddTile(tile.Color);
            if (tempSlotIndex < 0)
            {
                EvaluateStateAfterAction();
                return;
            }

            ResolveImmediatePatternMatch(tile.Color, tempSlotIndex);
            EvaluateStateAfterAction();
        }

        private void ResolveImmediatePatternMatch(BlockColor selectedColor, int tempSlotIndex)
        {
            PatternResolveResult result = patternController.ResolveAgainstBottomRow(selectedColor);
            if (!result.Matched)
            {
                return;
            }

            if (result.IsCaseA)
            {
                // Case A: keep tile in Temp Zone, only mark progress as 1/3 or 2/3.
                tempZoneController.MarkCaseAProgress(selectedColor, tempSlotIndex, result.PatternRemovedCount);
                return;
            }

            // Case B: remove 3 matching tiles from Temp Zone.
            tempZoneController.RemoveByColor(selectedColor, 3);
        }

        private void EvaluateStateAfterAction()
        {
            if (State != GameState.Running)
            {
                return;
            }

            if (patternController.IsEmpty)
            {
                State = GameState.Won;
                Debug.Log("Victory: Entire Pattern is cleared.");
                return;
            }

            IReadOnlyList<BlockColor> bottomRow = patternController.GetBottomRowColors();
            var bottomColorSet = new HashSet<BlockColor>(bottomRow);

            bool hasBottomMatchInTemp = tempZoneController.HasAnyColorInSet(bottomColorSet);
            if (tempZoneController.IsFull && !hasBottomMatchInTemp)
            {
                State = GameState.Failed;
                Debug.Log("Fail: Temp Zone is full and no Temp Zone color matches current Pattern bottom row.");
                return;
            }

            Debug.Log($"Game running. PatternBottom=[{string.Join(",", bottomRow)}], TempCount={tempZoneController.Count}/{tempZoneController.Capacity}, Rescue={rescueUses}/{gameConfig.MaxRescueUses}");
        }
    }
}
