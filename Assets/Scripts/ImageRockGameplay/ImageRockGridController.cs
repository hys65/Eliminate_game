using System.Collections.Generic;
using System.Linq;
using EliminateGame.Visual;
using UnityEngine;

namespace EliminateGame.ImageRockGameplay
{
    public sealed class ImageRockGridController : MonoBehaviour
    {
        [SerializeField] private Transform cellRoot;
        [SerializeField] private int width = 30;
        [SerializeField] private int height = 28;
        [SerializeField] private float cellSize = 0.18f;
        [SerializeField] private float spacing = 0.01f;
        [SerializeField] private int sortingOrderBase = 500;
        [SerializeField] private bool buildOnStart = false;
        [SerializeField] private LargePatternVisualConfig visualConfig;
        [SerializeField] private bool buildFromVisualConfig = true;

        private ImageRockCell[,] cells;
        private static Sprite generatedSprite;

        private void Start()
        {
            if (buildOnStart)
            {
                BuildGrid();
            }
        }

        public void BuildGrid()
        {
            ClearGrid();
            if (buildFromVisualConfig && visualConfig != null && visualConfig.ValidateSize())
            {
                BuildGridFromVisualConfig();
            }
            else
            {
                BuildFallbackGrid();
            }
        }

        private void BuildGridFromVisualConfig()
        {
            if (cellRoot == null) cellRoot = transform;
            width = visualConfig.Width;
            height = visualConfig.Height;
            cellSize = visualConfig.CellSize;
            cells = new ImageRockCell[width, height];
            Sprite sprite = GetGeneratedSprite();

            for (int dataY = 0; dataY < height; dataY++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!TryGetConfigCell(x, dataY, out Color displayColor, out int paletteIndex)) continue;
                    if (paletteIndex == LargePatternVisualConfig.TransparentPaletteIndex || displayColor.a <= 0.01f) continue;

                    ImageRockColor gameplayColor = MapPaletteColorToRockColor(displayColor);
                    if (gameplayColor == ImageRockColor.None) continue;

                    int y = ConvertDataYToRenderRow(dataY);
                    CreateCell(x, y, gameplayColor, displayColor, sprite);
                }
            }
        }

        private void BuildFallbackGrid()
        {
            if (cellRoot == null) cellRoot = transform;
            cells = new ImageRockCell[width, height];
            ImageRockColor[,] layout = CreateMonkeyLikeLayout();
            Sprite sprite = GetGeneratedSprite();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    ImageRockColor color = layout[x, y];
                    if (color == ImageRockColor.None) continue;
                    CreateCell(x, y, color, ToDisplayColor(color), sprite);
                }
            }
        }

        private void CreateCell(int x, int y, ImageRockColor gameplayColor, Color displayColor, Sprite sprite)
        {
            GameObject go = new GameObject($"ImageRockCell_{x:00}_{y:00}");
            go.transform.SetParent(cellRoot, false);
            ImageRockCell cell = go.AddComponent<ImageRockCell>();
            cell.Initialize(x, y, gameplayColor, sprite, displayColor, sortingOrderBase + y);
            cell.SetGridPosition(x, y, GetLocalPosition(x, y));
            go.transform.localScale = Vector3.one * cellSize;
            cells[x, y] = cell;
        }

        public void ResetGrid()
        {
            BuildGrid();
        }

        public void ClearGrid()
        {
            if (cellRoot == null) cellRoot = transform;
            for (int i = cellRoot.childCount - 1; i >= 0; i--)
            {
                DestroyChild(cellRoot.GetChild(i).gameObject);
            }
            cells = null;
        }

        public int GetRemainingCount()
        {
            int count = 0;
            ForEachCell(cell => { if (!cell.IsRemoved) count++; });
            return count;
        }


        public Dictionary<ImageRockColor, int> GetInitialColorCounts()
        {
            Dictionary<ImageRockColor, int> counts = CreateColorDictionary();
            ForEachCell(cell => { if (cell.Color != ImageRockColor.None) counts[cell.Color]++; });
            return counts;
        }

        public Dictionary<ImageRockColor, int> GetRemainingColorCounts()
        {
            Dictionary<ImageRockColor, int> counts = CreateColorDictionary();
            ForEachCell(cell => { if (!cell.IsRemoved) counts[cell.Color]++; });
            return counts;
        }

        public Dictionary<ImageRockColor, int> GetBottomExposedColorCounts()
        {
            Dictionary<ImageRockColor, int> counts = CreateColorDictionary();
            foreach (ImageRockCell cell in GetBottomExposedCells()) counts[cell.Color]++;
            return counts;
        }

        public int RemoveBottomExposedRocks(ImageRockColor color, int maxCount)
        {
            if (color == ImageRockColor.None || maxCount <= 0) return 0;
            List<ImageRockCell> targets = GetBottomExposedCells()
                .Where(cell => cell.Color == color)
                .OrderByDescending(cell => cell.Y)
                .ThenBy(cell => cell.X)
                .Take(maxCount)
                .ToList();
            foreach (ImageRockCell target in targets) target.Remove();
            return targets.Count;
        }

        public void ApplyColumnGravity()
        {
            if (cells == null) return;
            ImageRockCell[,] next = new ImageRockCell[width, height];
            for (int x = 0; x < width; x++)
            {
                List<ImageRockCell> column = new List<ImageRockCell>();
                for (int y = 0; y < height; y++)
                {
                    ImageRockCell cell = cells[x, y];
                    if (cell != null && !cell.IsRemoved) column.Add(cell);
                }
                int writeY = height - 1;
                for (int i = column.Count - 1; i >= 0; i--)
                {
                    ImageRockCell cell = column[i];
                    cell.SetGridPosition(x, writeY, GetLocalPosition(x, writeY));
                    next[x, writeY] = cell;
                    writeY--;
                }
            }
            cells = next;
        }

        private List<ImageRockCell> GetBottomExposedCells()
        {
            List<ImageRockCell> exposed = new List<ImageRockCell>();
            if (cells == null) return exposed;
            for (int x = 0; x < width; x++)
            {
                for (int y = height - 1; y >= 0; y--)
                {
                    ImageRockCell cell = cells[x, y];
                    if (cell != null && !cell.IsRemoved)
                    {
                        exposed.Add(cell);
                        break;
                    }
                }
            }
            return exposed;
        }

        private void ForEachCell(System.Action<ImageRockCell> action)
        {
            if (cells == null) return;
            for (int x = 0; x < width; x++) for (int y = 0; y < height; y++) if (cells[x, y] != null) action(cells[x, y]);
        }

        private ImageRockColor[,] CreateMonkeyLikeLayout()
        {
            ImageRockColor[,] layout = new ImageRockColor[width, height];
            ImageRockColor[] palette = { ImageRockColor.Brown, ImageRockColor.Dark, ImageRockColor.Green, ImageRockColor.Cream, ImageRockColor.Pink, ImageRockColor.Yellow, ImageRockColor.White, ImageRockColor.Blue };
            for (int y = 0; y < height; y++)
            {
                float center = (width - 1) * 0.5f;
                float half = y < 5 ? 4 + y * 0.9f : y < 19 ? 12.8f - Mathf.Abs(y - 12) * 0.23f : 12.5f - (y - 19) * 0.58f;
                for (int x = 0; x < width; x++)
                {
                    bool ear = (y >= 6 && y <= 13) && (Mathf.Abs(x - 3) <= 2 || Mathf.Abs(x - 26) <= 2);
                    bool body = Mathf.Abs(x - center) <= half;
                    if (!body && !ear) continue;
                    if ((y == 9 || y == 10) && (x == 10 || x == 19)) layout[x, y] = ImageRockColor.Dark;
                    else if (y >= 13 && y <= 17 && x >= 11 && x <= 18) layout[x, y] = ImageRockColor.Cream;
                    else layout[x, y] = palette[(x + y * 2) % palette.Length];
                }
            }
            return layout;
        }


        private bool TryGetConfigCell(int x, int dataY, out Color color, out int paletteIndex)
        {
            color = Color.clear;
            paletteIndex = LargePatternVisualConfig.TransparentPaletteIndex;
            if (visualConfig == null) return false;
            if (visualConfig.HasValidPaletteData) return visualConfig.TryGetPaletteCellColor(x, dataY, out color, out paletteIndex);
            color = ToDisplayColor(ImageRockColor.Brown);
            paletteIndex = -1;
            return visualConfig.GetCell(x, dataY) != EliminateGame.Pattern.BlockColor.None;
        }

        private int ConvertDataYToRenderRow(int dataY)
        {
            return height - 1 - dataY;
        }

        private ImageRockColor MapPaletteColorToRockColor(Color color)
        {
            if (color.a <= 0.01f) return ImageRockColor.None;
            Color.RGBToHSV(color, out float hue, out float saturation, out float value);
            if (value < 0.25f) return ImageRockColor.Dark;
            if (color.g > color.r && color.g > color.b && saturation > 0.25f) return ImageRockColor.Green;
            if (color.r > 0.7f && color.b > 0.5f) return ImageRockColor.Pink;
            if (color.r > 0.65f && color.g > 0.45f && color.b < 0.35f) return ImageRockColor.Brown;
            if (color.r > 0.75f && color.g > 0.65f && color.b < 0.35f) return ImageRockColor.Yellow;
            if (color.b > color.r && color.b > color.g) return ImageRockColor.Blue;
            if (color.r > 0.75f && color.g > 0.65f && color.b > 0.5f) return ImageRockColor.Cream;
            if (hue >= 0.10f && hue <= 0.18f && saturation > 0.25f) return ImageRockColor.Yellow;
            return ImageRockColor.Brown;
        }

        private Vector3 GetLocalPosition(int x, int y)
        {
            float step = cellSize + spacing;
            return new Vector3((x - (width - 1) * 0.5f) * step, ((height - 1) * 0.5f - y) * step, 0f);
        }

        private static Dictionary<ImageRockColor, int> CreateColorDictionary() => new Dictionary<ImageRockColor, int>
        {
            { ImageRockColor.Brown, 0 }, { ImageRockColor.Dark, 0 }, { ImageRockColor.Green, 0 }, { ImageRockColor.Cream, 0 },
            { ImageRockColor.Pink, 0 }, { ImageRockColor.Yellow, 0 }, { ImageRockColor.White, 0 }, { ImageRockColor.Blue, 0 }
        };

        internal static Sprite GetGeneratedSprite()
        {
            if (generatedSprite != null) return generatedSprite;
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            generatedSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return generatedSprite;
        }

        internal static Color ToDisplayColor(ImageRockColor color)
        {
            switch (color)
            {
                case ImageRockColor.Brown: return new Color(0.46f, 0.25f, 0.12f);
                case ImageRockColor.Dark: return new Color(0.12f, 0.09f, 0.07f);
                case ImageRockColor.Green: return new Color(0.18f, 0.55f, 0.25f);
                case ImageRockColor.Cream: return new Color(0.88f, 0.72f, 0.48f);
                case ImageRockColor.Pink: return new Color(0.95f, 0.45f, 0.64f);
                case ImageRockColor.Yellow: return new Color(0.96f, 0.78f, 0.20f);
                case ImageRockColor.White: return new Color(0.92f, 0.88f, 0.80f);
                case ImageRockColor.Blue: return new Color(0.25f, 0.48f, 0.86f);
                default: return Color.clear;
            }
        }
        private static void DestroyChild(GameObject child)
        {
            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }

    }
}
