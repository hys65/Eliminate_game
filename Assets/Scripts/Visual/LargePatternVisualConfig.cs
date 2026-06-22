using System.Collections.Generic;
using EliminateGame.Pattern;
using UnityEngine;

namespace EliminateGame.Visual
{
    [CreateAssetMenu(fileName = "LargePatternVisualConfig", menuName = "EliminateGame/Visual/Large Pattern Visual Config")]
    public class LargePatternVisualConfig : ScriptableObject
    {
        public const int TransparentPaletteIndex = 0;

        private const int DefaultWidth = 30;
        private const int DefaultHeight = 28;
        private const float DefaultCellSize = 0.18f;
        private const float TransparentAlphaCutoff = 0.001f;

        [Header("Visual-only prototype data")]
        [SerializeField, Min(1)] private int width = DefaultWidth;
        [SerializeField, Min(1)] private int height = DefaultHeight;
        [SerializeField, Min(0.01f)] private float cellSize = DefaultCellSize;

        [Tooltip("Legacy visual-only cells kept for backward compatibility with existing assets. New image generation writes palette data instead.")]
        [SerializeField] private List<BlockColor> cells = new List<BlockColor>(DefaultWidth * DefaultHeight);

        [Header("Visual-only palette data")]
        [Tooltip("Visual-only palette. Index 0 is reserved for None / Transparent in the default palette.")]
        [SerializeField] private List<Color> paletteColors = CreateDefaultPaletteColors();

        [Tooltip("Visual-only palette index per visual cell. Index 0 or a transparent palette color means None / blank.")]
        [SerializeField] private List<int> cellPaletteIndices = new List<int>(DefaultWidth * DefaultHeight);

        [TextArea]
        [SerializeField]
        private string visualOnlyNote = "Visual-only pixel pattern data. Do not use for GameConfig Pattern, PatternCount, deterministic solvability validation, runtime invariant validation, or win/lose state. Palette colors are visual-only and must not change gameplay BlockColor semantics.";

        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;
        public IReadOnlyList<BlockColor> Cells => cells;
        public IReadOnlyList<Color> PaletteColors => paletteColors;
        public IReadOnlyList<int> CellPaletteIndices => cellPaletteIndices;
        public string VisualOnlyNote => visualOnlyNote;
        public bool HasValidPaletteData => ValidatePaletteData();

        public BlockColor GetCell(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return BlockColor.None;
            }

            int index = (y * width) + x;
            if (index < 0 || cells == null || index >= cells.Count)
            {
                return BlockColor.None;
            }

            return cells[index];
        }

        public bool TryGetPaletteCellColor(int x, int y, out Color color)
        {
            return TryGetPaletteCellColor(x, y, out color, out _);
        }

        public bool TryGetPaletteCellColor(int x, int y, out Color color, out int paletteIndex)
        {
            color = Color.clear;
            paletteIndex = TransparentPaletteIndex;
            if (!ValidatePaletteData() || x < 0 || x >= width || y < 0 || y >= height)
            {
                return false;
            }

            int cellIndex = (y * width) + x;
            paletteIndex = cellPaletteIndices[cellIndex];
            if (paletteIndex < 0 || paletteIndex >= paletteColors.Count)
            {
                return false;
            }

            color = paletteColors[paletteIndex];
            return color.a > TransparentAlphaCutoff;
        }

        public bool ValidateSize()
        {
            return width > 0
                && height > 0
                && (ValidatePaletteData() || ValidateLegacyData());
        }

        public bool ValidatePaletteData()
        {
            int expectedCount = width * height;
            return width > 0
                && height > 0
                && paletteColors != null
                && paletteColors.Count > 0
                && cellPaletteIndices != null
                && cellPaletteIndices.Count == expectedCount;
        }

        public static List<Color> CreateDefaultPaletteColors()
        {
            return new List<Color>
            {
                Color.clear,
                HexToColor("#2B2B2B"),
                HexToColor("#F6F4F0"),
                HexToColor("#CFCFD4"),
                HexToColor("#F7B8D8"),
                HexToColor("#F2C4A2"),
                HexToColor("#F3E4B7"),
                HexToColor("#C98D59"),
                HexToColor("#8A5A3C"),
                HexToColor("#E84A5F"),
                HexToColor("#F39A2B"),
                HexToColor("#F4E55A"),
                HexToColor("#59C36A"),
                HexToColor("#49B7A5"),
                HexToColor("#6EC8FF"),
                HexToColor("#4D79D8"),
                HexToColor("#8B5BD6")
            };
        }

        public void SetPaletteVisualData(int newWidth, int newHeight, float newCellSize, List<Color> newPaletteColors, List<int> newCellPaletteIndices)
        {
            width = Mathf.Max(1, newWidth);
            height = Mathf.Max(1, newHeight);
            cellSize = Mathf.Max(0.01f, newCellSize);
            paletteColors = newPaletteColors != null
                ? new List<Color>(newPaletteColors)
                : CreateDefaultPaletteColors();
            cellPaletteIndices = newCellPaletteIndices != null
                ? new List<int>(newCellPaletteIndices)
                : new List<int>();

            EnsurePaletteContainsTransparentEntry();
            NormalizePaletteIndexCount();
            NormalizeLegacyCellsForCompatibility();
            visualOnlyNote = "Visual-only pixel pattern data. Do not use for GameConfig Pattern, PatternCount, deterministic solvability validation, runtime invariant validation, or win/lose state. Palette colors are visual-only and must not change gameplay BlockColor semantics.";
        }

        private void OnValidate()
        {
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);
            cellSize = Mathf.Max(0.01f, cellSize);

            if (paletteColors == null)
            {
                paletteColors = CreateDefaultPaletteColors();
            }

            EnsurePaletteContainsTransparentEntry();
            NormalizeLegacyCellsForCompatibility();

            if (cellPaletteIndices == null)
            {
                cellPaletteIndices = new List<int>(width * height);
            }

            if (cellPaletteIndices.Count > 0)
            {
                NormalizePaletteIndexCount();
            }
        }

        private bool ValidateLegacyData()
        {
            return cells != null && cells.Count == width * height;
        }

        private void EnsurePaletteContainsTransparentEntry()
        {
            if (paletteColors == null)
            {
                paletteColors = CreateDefaultPaletteColors();
            }

            if (paletteColors.Count == 0)
            {
                paletteColors.Add(Color.clear);
            }

            paletteColors[TransparentPaletteIndex] = Color.clear;
        }

        private void NormalizeLegacyCellsForCompatibility()
        {
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

        private void NormalizePaletteIndexCount()
        {
            if (cellPaletteIndices == null)
            {
                cellPaletteIndices = new List<int>(width * height);
            }

            int expectedCount = width * height;
            while (cellPaletteIndices.Count < expectedCount)
            {
                cellPaletteIndices.Add(TransparentPaletteIndex);
            }

            if (cellPaletteIndices.Count > expectedCount)
            {
                cellPaletteIndices.RemoveRange(expectedCount, cellPaletteIndices.Count - expectedCount);
            }

            for (int i = 0; i < cellPaletteIndices.Count; i++)
            {
                int paletteIndex = cellPaletteIndices[i];
                if (paletteIndex < 0 || paletteIndex >= paletteColors.Count)
                {
                    cellPaletteIndices[i] = TransparentPaletteIndex;
                }
            }
        }

        private static Color HexToColor(string htmlColor)
        {
            return ColorUtility.TryParseHtmlString(htmlColor, out Color color) ? color : Color.magenta;
        }
    }
}
