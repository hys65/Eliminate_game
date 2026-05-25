using System.Collections.Generic;
using System.Linq;
using EliminateGame.Data;
using EliminateGame.Pattern;
using EliminateGame.SelectionArea;
using EliminateGame.TempZone;
using EliminateGame.Validation;
using UnityEngine;

namespace EliminateGame.Core
{
    public class GameManager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private LevelDatabase levelDatabase;
        [SerializeField] private int currentLevelIndex;

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
        private GUIStyle restartButtonStyle;
        private bool isMenuOpen;
        private GameConfig activeGameConfig;
        private int resolveClickSequenceId;
        private int resolveStepSequenceId;

        public GameState State { get; private set; } = GameState.None;
        public int RescueUses => rescueUses;
        public int RescueUsesLeft => activeGameConfig != null ? Mathf.Max(0, activeGameConfig.MaxRescueUses - rescueUses) : 0;

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
            if (patternController == null || tempZoneController == null || selectionAreaGridController == null)
            {
                Debug.LogError("Missing references on GameManager.");
                return;
            }

            GameConfig activeConfig = ResolveActiveGameConfig();
            if (activeConfig == null)
            {
                Debug.LogError("No valid GameConfig resolved for GameManager.StartRun.");
                return;
            }

            activeGameConfig = activeConfig;

            bool validationPassed =
                DeterministicSolvabilityValidator.Validate(
                    activeConfig,
                    "GameManager.StartRun");

            if (!validationPassed)
            {
                Debug.LogError(
                    "Game start aborted because deterministic solvability validation failed.");

                return;
            }

            rescueUses = 0;
            State = GameState.Running;
            isMenuOpen = false;

            var patternRows = activeConfig.PatternRows
                .Select(row => (IReadOnlyList<BlockColor>)row.Cells)
                .ToList();

            patternController.Initialize(patternRows);
            tempZoneController.Initialize(activeConfig.TempZoneCapacity);

            selectionAreaGridController.TileSelected -= OnSelectionAreaTileSelected;
            selectionAreaGridController.Initialize(activeConfig);
            selectionAreaGridController.TileSelected += OnSelectionAreaTileSelected;

            Debug.Log("Game run started.");
            AssertGameRuntimeSafety("StartRun");
            EvaluateStateAfterAction();
        }


        private GameConfig ResolveActiveGameConfig()
        {
            if (levelDatabase != null)
            {
                if (levelDatabase.TryGetLevel(currentLevelIndex, out GameConfig levelConfig))
                {
                    return levelConfig;
                }

                return levelDatabase.GetDefaultLevel();
            }

            return gameConfig;
        }
        [ContextMenu("Try Use Rescue")]
        public bool TryUseRescue()
        {
            if (State != GameState.Running)
            {
                Debug.Log("Rescue ignored. Game is not running.");
                return false;
            }

            if (activeGameConfig == null || rescueUses >= activeGameConfig.MaxRescueUses)
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
            Debug.Log($"Rescue used. Count={rescueUses}/{activeGameConfig.MaxRescueUses}");
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

            resolveClickSequenceId++;
            resolveStepSequenceId = 0;
            Debug.Log($"[RESOLVE_DEBUG] OnSelectionAreaTileSelected clickedColor={tile.Color} tempZoneCountBeforeAdd={tempZoneController.Count}");
            int tempSlotIndex = tempZoneController.AddTile(tile.Color);
            if (tempSlotIndex < 0)
            {
                Debug.Log($"[RESOLVE_DEBUG] OnSelectionAreaTileSelected addFailed tempZoneCountAfterAdd={tempZoneController.Count}");
                EvaluateStateAfterAction();
                return;
            }

            Debug.Log($"[RESOLVE_DEBUG] OnSelectionAreaTileSelected tempZoneCountAfterAdd={tempZoneController.Count} addedSlotIndex={tempSlotIndex}");

            AssertGameRuntimeSafety("OnSelectionAreaTileSelected.BeforeConsume", tile.Color, tempSlotIndex);
            selectionAreaGridController.ConsumeTileAndUnlockCrossNeighbors(tile);
            ResolvePatternUsingTempZoneChain(tile.Color, tempSlotIndex);
            AssertGameRuntimeSafety("OnSelectionAreaTileSelected.AfterResolve", tile.Color, tempSlotIndex, validateSlotIndex: false);
            EvaluateStateAfterAction();
        }

        private void ResolvePatternUsingTempZoneChain(BlockColor selectedColor, int tempSlotIndex)
        {
            TryResolveSelectedColorFirst(selectedColor, tempSlotIndex);

            int iterationLimit = Mathf.Max(1, autoResolveSafetyLimit);
            for (int iteration = 0; iteration < iterationLimit; iteration++)
            {
                IReadOnlyList<BlockColor> loopBottomRowColors = patternController.GetBottomRowColors();
                string bottomRowColorLog = loopBottomRowColors.Count > 0 ? string.Join(",", loopBottomRowColors) : "<empty>";
                string tempSlotLog = tempZoneController.Slots.Count > 0
                    ? string.Join(" | ", tempZoneController.Slots.Select((slot, idx) => $"[{idx}] {slot.Color} p={slot.ProgressMark}"))
                    : "<empty>";

                bool hasMatch = tempZoneController.TryFindMatchingSlot(new HashSet<BlockColor>(loopBottomRowColors), out int matchingSlotIndex, out BlockColor matchingColor);
                Debug.Log($"[RESOLVE_DEBUG] ResolvePatternUsingTempZoneChain iteration={iteration} bottomRowColors=[{bottomRowColorLog}] tempSlots=[{tempSlotLog}] tryFindMatchingSlot={hasMatch} matchingSlotIndex={matchingSlotIndex} matchingColor={matchingColor}");

                if (!TryResolveAnyTempSlotForCurrentBottomRow())
                {
                    break;
                }
            }

        }

        private bool TryResolveSelectedColorFirst(BlockColor selectedColor, int tempSlotIndex)
        {
            int effectiveSlotIndex = tempSlotIndex;
            if (!IsValidSlotIndexWithColor(effectiveSlotIndex, selectedColor))
            {
                effectiveSlotIndex = FindFirstSlotIndexByColor(selectedColor);
            }

            if (effectiveSlotIndex < 0)
            {
                return false;
            }

            return ResolveAgainstTempSlot(selectedColor, effectiveSlotIndex);
        }

        private bool TryResolveAnyTempSlotForCurrentBottomRow()
        {
            IReadOnlyList<BlockColor> bottomRowColors = patternController.GetBottomRowColors();
            if (bottomRowColors.Count == 0)
            {
                return false;
            }

            if (!tempZoneController.TryFindMatchingSlot(new HashSet<BlockColor>(bottomRowColors), out int slotIndex, out BlockColor color))
            {
                return false;
            }

            string bottomRowColorLog = bottomRowColors.Count > 0 ? string.Join(",", bottomRowColors) : "<empty>";

            return ResolveAgainstTempSlot(color, slotIndex);
        }

        private bool IsValidSlotIndexWithColor(int slotIndex, BlockColor color)
        {
            return slotIndex >= 0
                   && slotIndex < tempZoneController.Slots.Count
                   && tempZoneController.Slots[slotIndex].Color == color;
        }

        private int FindFirstSlotIndexByColor(BlockColor color)
        {
            for (int i = 0; i < tempZoneController.Slots.Count; i++)
            {
                if (tempZoneController.Slots[i].Color == color)
                {
                    return i;
                }
            }

            return -1;
        }


        private Dictionary<BlockColor, int> BuildPatternAndTempZoneColorCounts()
        {
            var counts = new Dictionary<BlockColor, int>();

            Dictionary<BlockColor, int> patternCounts = patternController.GetNonNoneColorCounts();
            foreach (KeyValuePair<BlockColor, int> pair in patternCounts)
            {
                counts[pair.Key] = counts.GetValueOrDefault(pair.Key, 0) + pair.Value;
            }

            for (int i = 0; i < tempZoneController.Slots.Count; i++)
            {
                BlockColor color = tempZoneController.Slots[i].Color;
                if (color == BlockColor.None)
                {
                    continue;
                }

                counts[color] = counts.GetValueOrDefault(color, 0) + 1;
            }

            return counts;
        }

        private void AssertGameRuntimeSafety(string context, BlockColor color = BlockColor.None, int slotIndex = -1, bool validateSlotIndex = true)
        {
            Debug.Assert(
                tempZoneController.Count >= 0,
                $"[SAFETY][GameManager] Negative TempZone count in {context} for color={color}. TempCount={tempZoneController.Count}.");

            if (validateSlotIndex)
            {
                Debug.Assert(
                    slotIndex < 0 || (slotIndex >= 0 && slotIndex < tempZoneController.Slots.Count),
                    $"[SAFETY][GameManager] Invalid slot index in {context} for color={color}. SlotIndex={slotIndex}, SlotCount={tempZoneController.Slots.Count}.");
            }

            Dictionary<BlockColor, int> totalCounts = BuildPatternAndTempZoneColorCounts();
            foreach (KeyValuePair<BlockColor, int> pair in totalCounts)
            {
                Debug.Assert(
                    pair.Value >= 0,
                    $"[SAFETY][GameManager] Negative total count in {context} for color={pair.Key}. TotalCount={pair.Value}.");
            }
        }


        private void AssertColorConsistencyAfterResolve(
            Dictionary<BlockColor, int> beforeCounts,
            Dictionary<BlockColor, int> afterCounts,
            BlockColor targetColor,
            int expectedDelta,
            string context)
        {
            int beforeTarget = beforeCounts.GetValueOrDefault(targetColor, 0);
            int afterTarget = afterCounts.GetValueOrDefault(targetColor, 0);
            int actualDelta = beforeTarget - afterTarget;
            Debug.Assert(
                actualDelta == expectedDelta,
                $"[SAFETY][GameManager] Pattern+TempZone color count mismatch in {context} for color={targetColor}. Before={beforeTarget}, After={afterTarget}, ExpectedDelta={expectedDelta}, ActualDelta={actualDelta}.");
        }

        private bool ResolveAgainstTempSlot(BlockColor selectedColor, int tempSlotIndex)
        {
            resolveStepSequenceId++;

            int bottomRowCount = patternController.GetBottomRowCount(selectedColor);
            int targetSlotProgress = tempZoneController.Slots[tempSlotIndex].ProgressMark;
            int remainingProgress = Mathf.Clamp(3 - targetSlotProgress, 0, 3);
            int removeCount = bottomRowCount < 3
                ? bottomRowCount
                : Mathf.Min(bottomRowCount, remainingProgress);
            string patternCountsBefore = FormatColorCounts(patternController.GetNonNoneColorCounts());
            string tempSlotsBefore = FormatTempSlots();


            Debug.Log(
            Debug.Log($"[RESOLVE_DEBUG] ResolveAgainstTempSlot beforeResolve selectedColor={selectedColor} tempSlotIndex={tempSlotIndex}");
            AssertGameRuntimeSafety("ResolveAgainstTempSlot.BeforePatternResolve", selectedColor, tempSlotIndex);
            Dictionary<BlockColor, int> beforeCounts = BuildPatternAndTempZoneColorCounts();
            PatternResolveResult result = patternController.ResolveAgainstBottomRowWithLimit(selectedColor, removeCount);
            string patternCountsAfterPatternResolve = FormatColorCounts(patternController.GetNonNoneColorCounts());
            string tempSlotsBeforeMutation = FormatTempSlots();
            Debug.Log(
            Debug.Log($"[RESOLVE_DEBUG] ResolveAgainstTempSlot afterResolve matched={result.Matched} isCaseA={result.IsCaseA} patternRemovedCount={result.PatternRemovedCount}");
            if (!result.Matched)
            {
                return false;
            }

            tempZoneController.ApplyCaseAProgress(tempSlotIndex, result.PatternRemovedCount);
            Dictionary<BlockColor, int> afterCaseACounts = BuildPatternAndTempZoneColorCounts();
            AssertColorConsistencyAfterResolve(beforeCounts, afterCaseACounts, selectedColor, result.PatternRemovedCount, "ResolveAgainstTempSlot.AfterProgressResolve");
            AssertGameRuntimeSafety("ResolveAgainstTempSlot.AfterProgressResolve", selectedColor, tempSlotIndex);
            CleanupStaleTempZoneSlotsAfterPatternUpdate();
            return true;
        }


        private void CleanupStaleTempZoneSlotsAfterPatternUpdate()
        {
            tempZoneController.RemoveSlotsWhereColorNoLongerExists(patternController.ContainsColor);
        }

        private string FormatColorCounts(Dictionary<BlockColor, int> counts)
        {
            if (counts == null || counts.Count == 0)
            {
                return "<empty>";
            }

            return string.Join(",",
                counts
                    .Where(pair => pair.Key != BlockColor.None && pair.Value > 0)
                    .OrderBy(pair => pair.Key.ToString())
                    .Select(pair => $"{pair.Key}:{pair.Value}"));
        }

        private string FormatTempSlots()
        {
            if (tempZoneController == null || tempZoneController.Slots == null || tempZoneController.Slots.Count == 0)
            {
                return "<empty>";
            }

            return string.Join(" | ", tempZoneController.Slots.Select((slot, idx) => $"[{idx}] {slot.Color} p={slot.ProgressMark}"));
        }

        private string FormatSelectionRemainingCounts()
        {
            Dictionary<BlockColor, int> selectionCounts = selectionAreaGridController != null
                ? selectionAreaGridController.GetRemainingNonRemovedColorCounts()
                : null;

            return FormatColorCounts(selectionCounts);
        }

        private string BuildRemainingInvariantReport()
        {
            Dictionary<BlockColor, int> patternCounts = patternController != null
                ? patternController.GetNonNoneColorCounts()
                : new Dictionary<BlockColor, int>();
            Dictionary<BlockColor, int> selectionCounts = selectionAreaGridController != null
                ? selectionAreaGridController.GetRemainingNonRemovedColorCounts()
                : new Dictionary<BlockColor, int>();

            var allColors = new HashSet<BlockColor>();
            foreach (KeyValuePair<BlockColor, int> pair in patternCounts)
            {
                if (pair.Key != BlockColor.None && pair.Value > 0)
                {
                    allColors.Add(pair.Key);
                }
            }

            foreach (KeyValuePair<BlockColor, int> pair in selectionCounts)
            {
                if (pair.Key != BlockColor.None && pair.Value > 0)
                {
                    allColors.Add(pair.Key);
                }
            }

            if (tempZoneController != null && tempZoneController.Slots != null)
            {
                foreach (TempZoneSlot slot in tempZoneController.Slots)
                {
                    if (slot.Color != BlockColor.None)
                    {
                        allColors.Add(slot.Color);
                    }
                }
            }

            if (allColors.Count == 0)
            {
                return "<empty>";
            }

            IEnumerable<string> reports = allColors
                .OrderBy(color => color.ToString())
                .Select(color =>
                {
                    int patternRemaining = patternCounts.GetValueOrDefault(color, 0);
                    int selectionRemaining = selectionCounts.GetValueOrDefault(color, 0);
                    int tempDebt = 0;
                    if (tempZoneController != null && tempZoneController.Slots != null)
                    {
                        for (int i = 0; i < tempZoneController.Slots.Count; i++)
                        {
                            TempZoneSlot slot = tempZoneController.Slots[i];
                            if (slot.Color != color)
                            {
                                continue;
                            }

                            tempDebt += 3 - slot.ProgressMark;
                        }
                    }

                    int expectedPatternRemaining = (selectionRemaining * 3) + tempDebt;
                    int delta = patternRemaining - expectedPatternRemaining;
                    return $"Color={color} Pattern={patternRemaining} SelectionRemaining={selectionRemaining} TempDebt={tempDebt} ExpectedPattern={expectedPatternRemaining} Delta={delta}";
                });

            return string.Join(" || ", reports);
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
            bool showInGameMenu = State == GameState.Running || State == GameState.Won || State == GameState.Failed;
            if (showInGameMenu)
            {
                var menuButtonRect = new Rect(12f, 12f, 96f, 36f);
                if (GUI.Button(menuButtonRect, "Menu"))
                {
                    isMenuOpen = !isMenuOpen;
                }

                if (isMenuOpen)
                {
                    var panelRect = new Rect(12f, 52f, 120f, 52f);
                    GUI.Box(panelRect, string.Empty);

                    var restartMenuRect = new Rect(20f, 60f, 104f, 36f);
                    if (GUI.Button(restartMenuRect, "Restart"))
                    {
                        isMenuOpen = false;
                        StartRun();
                    }
                }
            }

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

            if (restartButtonStyle == null)
            {
                restartButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 28,
                    fontStyle = FontStyle.Bold
                };
            }

            string message = State == GameState.Won ? "WIN" : "LOSE";
            var labelRect = new Rect(0f, 0f, Screen.width, Screen.height);
            GUI.Label(labelRect, message, stateLabelStyle);

            const float buttonWidth = 220f;
            const float buttonHeight = 64f;
            float buttonX = (Screen.width - buttonWidth) * 0.5f;
            float buttonY = (Screen.height * 0.5f) + 64f;
            var buttonRect = new Rect(buttonX, buttonY, buttonWidth, buttonHeight);

            if (GUI.Button(buttonRect, "Restart", restartButtonStyle))
            {
                StartRun();
            }
        }
    }
}
