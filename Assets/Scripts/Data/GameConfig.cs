using System;
using System.Collections.Generic;
using EliminateGame.Pattern;
using UnityEngine;

namespace EliminateGame.Data
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "EliminateGame/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Pattern")]
        [SerializeField] private List<PatternRowDefinition> patternRows = new List<PatternRowDefinition>();

        [Header("Temp Zone")]
        [SerializeField, Min(1)] private int tempZoneCapacity = 7;

        [Header("Selection Area")]
        [SerializeField, Min(1)] private int selectionWidth = 6;
        [SerializeField, Min(1)] private int selectionHeight = 6;
        [SerializeField] private List<SelectionTileDefinition> selectionTiles = new List<SelectionTileDefinition>();

        [Header("Rescue")]
        [SerializeField, Range(0, 10)] private int maxRescueUses = 3;

        public IReadOnlyList<PatternRowDefinition> PatternRows => patternRows;
        public int TempZoneCapacity => tempZoneCapacity;
        public int SelectionWidth => selectionWidth;
        public int SelectionHeight => selectionHeight;
        public IReadOnlyList<SelectionTileDefinition> SelectionTiles => selectionTiles;
        public int MaxRescueUses => maxRescueUses;

        [Serializable]
        public class PatternRowDefinition
        {
            public List<BlockColor> Cells = new List<BlockColor>();
        }

        [Serializable]
        public class SelectionTileDefinition
        {
            [Min(0)] public int X;
            [Min(0)] public int Y;
            public BlockColor Color;
            public bool StartUnlocked;
        }
    }
}
