using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        [Header("Safety")]
        [SerializeField, Min(1)] private int autoResolveSafetyLimit = 128;

        [Header("Debug")]
        [SerializeField] private int rescueRandomSeed = 12345;

        private System.Random rescueRandom;
        private int rescueUses;
        private GUIStyle stateLabelStyle;

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

            if (!ValidateLevelData(out string validationError))
            {
                State = GameState.None;
                Debug.LogError(validationError);
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

            int tempSlotIndex = tempZoneController.AddTile(tile.Color);
            if (tempSlotIndex < 0)
            {
                EvaluateStateAfterAction();
                return;
            }

            selectionAreaGridController.ConsumeTileAndUnlockCrossNeighbors(tile);
            ResolvePatternUsingTempZoneChain(tile.Color, tempSlotIndex);
            EvaluateStateAfterAction();
        }

        private void ResolvePatternUsingTempZoneChain(BlockColor selectedColor, int tempSlotIndex)
        {
            ResolveAgainstTempSlot(selectedColor, tempSlotIndex);

            int iterationLimit = Mathf.Max(1, autoResolveSafetyLimit);
            for (int iteration = 0; iteration < iterationLimit; iteration++)
            {
                IReadOnlyList<BlockColor> bottomRowColors = patternController.GetBottomRowColors();
                if (bottomRowColors.Count == 0)
                {
                    break;
                }

                if (!tempZoneController.TryFindMatchingSlot(bottomRowColors, out int matchingSlotIndex, out BlockColor matchingColor))
                {
                    break;
                }

                bool resolved = ResolveAgainstTempSlot(matchingColor, matchingSlotIndex);
                if (!resolved)
                {
                    break;
                }
            }
        }

        private bool ResolveAgainstTempSlot(BlockColor selectedColor, int tempSlotIndex)
        {
            int bottomRowCount = patternController.GetBottomRowCount(selectedColor);
            if (bottomRowCount >= 3)
            {
                int sameColorCountInTemp = tempZoneController.Slots.Count(slot => slot.Color == selectedColor);
                if (sameColorCountInTemp < 3)
                {
                    Debug.Log($"Case B blocked for {selectedColor}. Temp Zone has {sameColorCountInTemp}/3 required tiles.");
                    return false;
                }
            }

            PatternResolveResult result = patternController.ResolveAgainstBottomRow(selectedColor);
            if (!result.Matched)
            {
                return false;
            }

            if (result.IsCaseA)
            {
                tempZoneController.ApplyCaseAProgress(tempSlotIndex, result.PatternRemovedCount);
                return true;
            }

            tempZoneController.RemoveByColor(selectedColor, 3);
            return true;
        }

        private bool ValidateLevelData(out string errorMessage)
        {
            var patternColorCounts = CreateZeroedColorMap();
            foreach (GameConfig.PatternRowDefinition row in gameConfig.PatternRows)
            {
                if (row?.Cells == null)
                {
                    continue;
                }

                foreach (BlockColor color in row.Cells)
                {
                    if (color == BlockColor.None)
                    {
                        continue;
                    }

                    if (!patternColorCounts.ContainsKey(color))
                    {
                        continue;
                    }

                    patternColorCounts[color]++;
                }
            }

            var selectionColorCounts = CreateZeroedColorMap();
            var seenPositions = new HashSet<Vector2Int>();
            foreach (GameConfig.SelectionTileDefinition tile in gameConfig.SelectionTiles)
            {
                if (tile == null || tile.Color == BlockColor.None)
                {
                    continue;
                }

                Vector2Int key = new Vector2Int(tile.X, tile.Y);
                if (!seenPositions.Add(key))
                {
                    continue;
                }

                if (!selectionColorCounts.ContainsKey(tile.Color))
                {
                    continue;
                }

                selectionColorCounts[tile.Color]++;
            }

            var invalidLines = new List<string>();
            foreach (BlockColor color in GetPlayableColors())
            {
                int pattern = patternColorCounts[color];
                int selection = selectionColorCounts[color];
                int required = selection * 3;

                if (pattern != required)
                {
                    invalidLines.Add($"Color {color.ToString().ToUpperInvariant()}: Pattern={pattern}, Selection={selection}, Required={required} → INVALID");
                }
            }

            if (invalidLines.Count > 0)
            {
                var builder = new StringBuilder();
                builder.AppendLine("Level validation failed. Gameplay is blocked due to invalid color counts.");
                foreach (string line in invalidLines)
                {
                    builder.AppendLine(line);
                }

                errorMessage = builder.ToString();
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        private static Dictionary<BlockColor, int> CreateZeroedColorMap()
        {
            var map = new Dictionary<BlockColor, int>();
            foreach (BlockColor color in GetPlayableColors())
            {
                map[color] = 0;
            }

            return map;
        }

        private static IEnumerable<BlockColor> GetPlayableColors()
        {
            return System.Enum.GetValues(typeof(BlockColor))
                .Cast<BlockColor>()
                .Where(color => color != BlockColor.None);
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
                tempZoneController.Clear();
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

        private void OnGUI()
        {
            if (State != GameState.Won && State != GameState.Failed)
            {
                return;
            }

            if (stateLabelStyle == null)
            {
                stateLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 48,
                    fontStyle = FontStyle.Bold,
                    normal =
                    {
                        textColor = Color.white
                    }
                };
            }

            string message = State == GameState.Won ? "WIN" : "LOSE";
            var labelRect = new Rect(0f, 0f, Screen.width, Screen.height);
            GUI.Label(labelRect, message, stateLabelStyle);
        }
    }
}
