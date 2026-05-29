#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EliminateGame.Core;
using EliminateGame.Data;
using EliminateGame.Pattern;
using EliminateGame.TempZone;
using EliminateGame.Validation;
using UnityEditor;
using UnityEngine;

namespace EliminateGame.Editor.Validation
{
    public static class CurrentConfigValidationMenu
    {
        private const string MenuPath = "Tools/Eliminate Game/Validate Current Config";
        private const string LogPrefix = "[EDITOR_VALIDATION]";

        [MenuItem(MenuPath)]
        public static void ValidateCurrentConfig()
        {
            GameManager gameManager = FindSceneGameManager();
            if (gameManager == null)
            {
                Debug.LogError($"{LogPrefix} No GameManager was found in the currently loaded editor scenes. Open the gameplay scene and assign a GameConfig on GameManager.");
                return;
            }

            GameConfig config = ResolveCurrentGameConfig(gameManager);
            if (config == null)
            {
                Debug.LogError($"{LogPrefix} No GameConfig found for GameManager '{gameManager.name}'. Assign GameManager.gameConfig or a LevelDatabase level/default level.");
                return;
            }

            TempZoneController tempZoneController = GetObjectReference<TempZoneController>(gameManager, "tempZoneController");
            IReadOnlyList<TempZoneSlot> tempSlots = tempZoneController != null
                ? tempZoneController.Slots
                : System.Array.Empty<TempZoneSlot>();

            List<string> errors = new List<string>();
            Dictionary<BlockColor, int> patternCounts = BuildPatternCounts(config);
            Dictionary<BlockColor, int> selectionCounts = BuildSelectionCounts(config);

            ValidatePatternNotEmpty(patternCounts, errors);
            ValidateInitialSelectableTile(config, errors);
            ValidateTempSlotProgress(tempSlots, errors);
            ValidateInvariant(patternCounts, selectionCounts, tempSlots, errors);

            if (errors.Count > 0)
            {
                Debug.LogError(BuildFailureLog(errors));
                return;
            }

            Debug.Log($"{LogPrefix} PASS");
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateCurrentConfigMenuEnabled()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static GameManager FindSceneGameManager()
        {
            return Resources.FindObjectsOfTypeAll<GameManager>()
                .Where(manager => manager != null)
                .Where(manager => manager.gameObject.scene.IsValid())
                .Where(manager => !EditorUtility.IsPersistent(manager))
                .OrderBy(manager => manager.gameObject.scene.buildIndex)
                .ThenBy(manager => manager.name)
                .FirstOrDefault();
        }

        private static GameConfig ResolveCurrentGameConfig(GameManager gameManager)
        {
            LevelDatabase levelDatabase = GetObjectReference<LevelDatabase>(gameManager, "levelDatabase");
            int currentLevelIndex = GetIntValue(gameManager, "currentLevelIndex");

            if (levelDatabase != null)
            {
                if (levelDatabase.TryGetLevel(currentLevelIndex, out GameConfig levelConfig))
                {
                    return levelConfig;
                }

                return levelDatabase.GetDefaultLevel();
            }

            return GetObjectReference<GameConfig>(gameManager, "gameConfig");
        }

        private static T GetObjectReference<T>(GameManager gameManager, string propertyName) where T : Object
        {
            SerializedObject serializedObject = new SerializedObject(gameManager);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            return property != null ? property.objectReferenceValue as T : null;
        }

        private static int GetIntValue(GameManager gameManager, string propertyName)
        {
            SerializedObject serializedObject = new SerializedObject(gameManager);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            return property != null ? property.intValue : 0;
        }

        private static Dictionary<BlockColor, int> BuildPatternCounts(GameConfig config)
        {
            Dictionary<BlockColor, int> counts = new Dictionary<BlockColor, int>();

            foreach (GameConfig.PatternRowDefinition row in config.PatternRows)
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

                    counts[color] = counts.GetValueOrDefault(color, 0) + 1;
                }
            }

            return counts;
        }

        private static Dictionary<BlockColor, int> BuildSelectionCounts(GameConfig config)
        {
            Dictionary<BlockColor, int> counts = new Dictionary<BlockColor, int>();
            HashSet<Vector2Int> seenPositions = new HashSet<Vector2Int>();

            foreach (GameConfig.SelectionTileDefinition tile in config.SelectionTiles)
            {
                if (tile == null || tile.Color == BlockColor.None)
                {
                    continue;
                }

                Vector2Int position = new Vector2Int(tile.X, tile.Y);
                if (!seenPositions.Add(position))
                {
                    continue;
                }

                counts[tile.Color] = counts.GetValueOrDefault(tile.Color, 0) + 1;
            }

            return counts;
        }

        private static void ValidatePatternNotEmpty(Dictionary<BlockColor, int> patternCounts, List<string> errors)
        {
            if (patternCounts.Values.Sum() > 0)
            {
                return;
            }

            errors.Add("Pattern is empty. Add at least one non-None Pattern cell to the current GameConfig.");
        }

        private static void ValidateInitialSelectableTile(GameConfig config, List<string> errors)
        {
            bool hasSelectableTile = config.SelectionTiles.Any(tile =>
                tile != null &&
                tile.Color != BlockColor.None &&
                tile.StartUnlocked);

            if (hasSelectableTile)
            {
                return;
            }

            errors.Add("SelectionArea has no initially selectable tile. Mark at least one non-None SelectionTileDefinition as StartUnlocked.");
        }

        private static void ValidateTempSlotProgress(IReadOnlyList<TempZoneSlot> tempSlots, List<string> errors)
        {
            for (int i = 0; i < tempSlots.Count; i++)
            {
                TempZoneSlot slot = tempSlots[i];
                if (slot == null)
                {
                    continue;
                }

                if (slot.ProgressMark >= 0 && slot.ProgressMark <= 2)
                {
                    continue;
                }

                errors.Add($"TempZone slot ProgressMark invalid: SlotIndex={i}, Color={slot.Color}, ProgressMark={slot.ProgressMark}, ExpectedRange=[0,2].");
            }
        }

        private static void ValidateInvariant(
            Dictionary<BlockColor, int> patternCounts,
            Dictionary<BlockColor, int> selectionCounts,
            IReadOnlyList<TempZoneSlot> tempSlots,
            List<string> errors)
        {
            bool invariantPassed = RuntimeInvariantValidator.Validate(
                patternCounts,
                selectionCounts,
                tempSlots,
                "Editor.ValidateCurrentConfig");

            if (invariantPassed)
            {
                return;
            }

            errors.Add(BuildEditorInvariantFailure(patternCounts, selectionCounts, tempSlots));
        }

        private static string BuildEditorInvariantFailure(
            Dictionary<BlockColor, int> patternCounts,
            Dictionary<BlockColor, int> selectionCounts,
            IReadOnlyList<TempZoneSlot> tempSlots)
        {
            HashSet<BlockColor> colors = new HashSet<BlockColor>(patternCounts.Keys);
            colors.UnionWith(selectionCounts.Keys);

            foreach (TempZoneSlot slot in tempSlots)
            {
                if (slot != null && slot.Color != BlockColor.None)
                {
                    colors.Add(slot.Color);
                }
            }

            foreach (BlockColor color in colors.OrderBy(color => (int)color))
            {
                if (color == BlockColor.None)
                {
                    continue;
                }

                int pattern = patternCounts.GetValueOrDefault(color, 0);
                int selection = selectionCounts.GetValueOrDefault(color, 0);
                int tempDebt = CalculateTempDebt(color, tempSlots);
                int expected = (selection * 3) + tempDebt;

                if (pattern == expected)
                {
                    continue;
                }

                StringBuilder builder = new StringBuilder();
                builder.AppendLine($"Color={color}");
                builder.AppendLine($"Pattern={pattern}");
                builder.AppendLine($"Selection={selection}");
                builder.AppendLine($"TempDebt={tempDebt}");
                builder.Append($"Expected={expected}");
                return builder.ToString();
            }

            return "Invariant validation failed, but no mismatching color could be identified.";
        }

        private static int CalculateTempDebt(BlockColor color, IReadOnlyList<TempZoneSlot> tempSlots)
        {
            int tempDebt = 0;

            for (int i = 0; i < tempSlots.Count; i++)
            {
                TempZoneSlot slot = tempSlots[i];
                if (slot == null || slot.Color != color)
                {
                    continue;
                }

                tempDebt += 3 - slot.ProgressMark;
            }

            return tempDebt;
        }

        private static string BuildFailureLog(List<string> errors)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(LogPrefix);

            for (int i = 0; i < errors.Count; i++)
            {
                if (i > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(errors[i]);
            }

            return builder.ToString();
        }
    }
}
#endif
