using System;
using System.Collections.Generic;
using EliminateGame.Pattern;
using UnityEngine;

namespace EliminateGame.Visual
{
    [CreateAssetMenu(fileName = "GameplayColorVisualMapping", menuName = "EliminateGame/Visual/Gameplay Color Visual Mapping")]
    public class GameplayColorVisualMapping : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public BlockColor GameplayColor;
            public Color DisplayColor = Color.white;
            public List<int> TargetPaletteIndices = new List<int>();
            public string Note;
        }

        [SerializeField] private List<Entry> mappings = CreateDefaultEntries();

        public IReadOnlyList<Entry> Mappings => mappings;

        public Color GetDisplayColor(BlockColor gameplayColor, Color fallback)
        {
            return TryGetEntry(gameplayColor, out Entry entry) ? entry.DisplayColor : fallback;
        }

        public bool TryGetTargetPaletteIndices(BlockColor gameplayColor, out IReadOnlyList<int> indices)
        {
            indices = null;
            if (!TryGetEntry(gameplayColor, out Entry entry) || entry.TargetPaletteIndices == null || entry.TargetPaletteIndices.Count == 0)
            {
                return false;
            }

            indices = entry.TargetPaletteIndices;
            return true;
        }

        public bool TryGetEntry(BlockColor gameplayColor, out Entry entry)
        {
            if (mappings != null)
            {
                for (int i = 0; i < mappings.Count; i++)
                {
                    Entry candidate = mappings[i];
                    if (candidate != null && candidate.GameplayColor == gameplayColor)
                    {
                        entry = candidate;
                        return true;
                    }
                }
            }

            entry = null;
            return false;
        }

        public void ResetToDefaultMapping()
        {
            mappings = CreateDefaultEntries();
        }

        private void OnValidate()
        {
            if (mappings == null || mappings.Count == 0)
            {
                mappings = CreateDefaultEntries();
            }
        }

        public static List<Entry> CreateDefaultEntries()
        {
            return new List<Entry>
            {
                new Entry { GameplayColor = BlockColor.Red, DisplayColor = HexToColor("#C98D59"), TargetPaletteIndices = new List<int> { 7, 8, 10 }, Note = "LightBrown / DarkBrown / Orange" },
                new Entry { GameplayColor = BlockColor.Blue, DisplayColor = HexToColor("#59C36A"), TargetPaletteIndices = new List<int> { 12, 13 }, Note = "Green / Teal" },
                new Entry { GameplayColor = BlockColor.Green, DisplayColor = HexToColor("#F3E4B7"), TargetPaletteIndices = new List<int> { 2, 5, 6 }, Note = "White / Peach / Cream" },
                new Entry { GameplayColor = BlockColor.Yellow, DisplayColor = HexToColor("#F7B8D8"), TargetPaletteIndices = new List<int> { 4, 9, 11 }, Note = "Pink / Red / Yellow" },
                new Entry { GameplayColor = BlockColor.Purple, DisplayColor = HexToColor("#2B2B2B"), TargetPaletteIndices = new List<int> { 1, 15, 16 }, Note = "Black / DarkBlue / Purple" }
            };
        }

        private static Color HexToColor(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out Color color) ? color : Color.white;
        }
    }
}
