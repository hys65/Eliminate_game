using System.Collections.Generic;
using EliminateGame.Pattern;
using UnityEngine;

namespace EliminateGame.Visual
{
    [CreateAssetMenu(fileName = "LargePatternVisualConfig", menuName = "EliminateGame/Visual/Large Pattern Visual Config")]
    public class LargePatternVisualConfig : ScriptableObject
    {
        private const int DefaultWidth = 30;
        private const int DefaultHeight = 28;
        private const float DefaultCellSize = 0.18f;

        [Header("Visual-only prototype data")]
        [SerializeField, Min(1)] private int width = DefaultWidth;
        [SerializeField, Min(1)] private int height = DefaultHeight;
        [SerializeField, Min(0.01f)] private float cellSize = DefaultCellSize;
        [SerializeField] private List<BlockColor> cells = new List<BlockColor>(DefaultWidth * DefaultHeight);

        [TextArea]
        [SerializeField]
        private string visualOnlyNote = "Visual-only pixel pattern data. Do not use for GameConfig Pattern, PatternCount, deterministic solvability validation, runtime invariant validation, or win/lose state.";

        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;
        public IReadOnlyList<BlockColor> Cells => cells;
        public string VisualOnlyNote => visualOnlyNote;

        public BlockColor GetCell(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return BlockColor.None;
            }

            int index = (y * width) + x;
            if (index < 0 || index >= cells.Count)
            {
                return BlockColor.None;
            }

            return cells[index];
        }

        public bool ValidateSize()
        {
            return width > 0 && height > 0 && cells != null && cells.Count == width * height;
        }

        private void OnValidate()
        {
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);
            cellSize = Mathf.Max(0.01f, cellSize);

            if (cells == null)
            {
                cells = new List<BlockColor>(width * height);
            }

            int expectedCount = width * height;
            while (cells.Count < expectedCount)
            {
                cells.Add(BlockColor.None);
            }

            if (cells.Count > expectedCount)
            {
                cells.RemoveRange(expectedCount, cells.Count - expectedCount);
            }
        }
    }
}
