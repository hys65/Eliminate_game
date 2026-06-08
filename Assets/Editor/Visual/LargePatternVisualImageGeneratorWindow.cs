using System;
using System.Collections.Generic;
using System.IO;
using EliminateGame.Pattern;
using EliminateGame.Visual;
using UnityEditor;
using UnityEngine;

namespace EliminateGame.Editor.Visual
{
    public class LargePatternVisualImageGeneratorWindow : EditorWindow
    {
        private const int OutputWidth = 30;
        private const int OutputHeight = 28;
        private const int ExpectedCellCount = OutputWidth * OutputHeight;
        private const float DefaultCellSize = 0.22f;
        private const float DefaultAlphaThreshold = 0.1f;
        private const float DefaultBackgroundTolerance = 0.22f;
        private const float DefaultDarkToNoneThreshold = 0.12f;
        private const float DefaultLightToNoneThreshold = 0.92f;
        private const int DefaultNoiseCleanupPasses = 2;
        private const int DefaultMinimumNeighborCount = 2;
        private const string DefaultOutputPath = "Assets/GameConfigs/Visual/LargePatternVisual_30x28_FromImage.asset";
        private const string SuccessLogPrefix = "[LargePatternVisualImageGenerator] Generated visual config:";

        private static readonly Color PurpleColor = new Color(0.6f, 0.2f, 0.8f);
        private static readonly ColorMapping[] ColorMappings =
        {
            new ColorMapping(BlockColor.Red, Color.red),
            new ColorMapping(BlockColor.Blue, Color.blue),
            new ColorMapping(BlockColor.Green, Color.green),
            new ColorMapping(BlockColor.Yellow, Color.yellow),
            new ColorMapping(BlockColor.Purple, PurpleColor)
        };

        [SerializeField] private Texture2D sourceImage;
        [SerializeField] private string outputPath = DefaultOutputPath;
        [SerializeField] private float cellSize = DefaultCellSize;
        [SerializeField] private float alphaThreshold = DefaultAlphaThreshold;
        [SerializeField] private bool backgroundToNone = true;
        [SerializeField] private BackgroundSampleMode backgroundSampleMode = BackgroundSampleMode.TopLeft;
        [SerializeField] private float backgroundTolerance = DefaultBackgroundTolerance;
        [SerializeField] private bool brightnessToNone = true;
        [SerializeField] private float darkToNoneThreshold = DefaultDarkToNoneThreshold;
        [SerializeField] private float lightToNoneThreshold = DefaultLightToNoneThreshold;
        [SerializeField] private int noiseCleanupPasses = DefaultNoiseCleanupPasses;
        [SerializeField] private int minimumNeighborCount = DefaultMinimumNeighborCount;
        [SerializeField] private string statusMessage = string.Empty;
        [SerializeField] private MessageType statusType = MessageType.None;

        [MenuItem("Tools/Eliminate Game/Visual/Generate Large Pattern From Image")]
        public static void ShowWindow()
        {
            LargePatternVisualImageGeneratorWindow window = GetWindow<LargePatternVisualImageGeneratorWindow>("Large Pattern From Image");
            window.minSize = new Vector2(460f, 190f);
            window.Show();
        }

        private void OnEnable()
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = DefaultOutputPath;
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Image-to-LargePatternVisualConfig Pipeline 1.1", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            sourceImage = (Texture2D)EditorGUILayout.ObjectField("Source Image", sourceImage, typeof(Texture2D), false);
            outputPath = EditorGUILayout.TextField("Output Path", outputPath);
            cellSize = EditorGUILayout.FloatField("Cell Size", cellSize);
            alphaThreshold = EditorGUILayout.Slider("Alpha Threshold", alphaThreshold, 0f, 1f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Visual Quality Cleanup", EditorStyles.boldLabel);
            backgroundToNone = EditorGUILayout.Toggle("Background To None", backgroundToNone);
            using (new EditorGUI.DisabledScope(!backgroundToNone))
            {
                backgroundSampleMode = (BackgroundSampleMode)EditorGUILayout.EnumPopup("Background Sample Mode", backgroundSampleMode);
                backgroundTolerance = EditorGUILayout.Slider("Background Tolerance", backgroundTolerance, 0f, 1f);
            }

            brightnessToNone = EditorGUILayout.Toggle("Brightness To None", brightnessToNone);
            using (new EditorGUI.DisabledScope(!brightnessToNone))
            {
                darkToNoneThreshold = EditorGUILayout.Slider("Dark To None Threshold", darkToNoneThreshold, 0f, 1f);
                lightToNoneThreshold = EditorGUILayout.Slider("Light To None Threshold", lightToNoneThreshold, 0f, 1f);
            }

            noiseCleanupPasses = EditorGUILayout.IntSlider("Noise Cleanup Passes", noiseCleanupPasses, 0, 4);
            minimumNeighborCount = EditorGUILayout.IntSlider("Minimum Neighbor Count", minimumNeighborCount, 1, 4);

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate"))
            {
                Generate();
            }

            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.HelpBox(statusMessage, statusType);
            }
        }

        private void Generate()
        {
            if (!ValidateInputs())
            {
                return;
            }

            if (!EnsureTextureReadable(ref sourceImage))
            {
                SetFailure("Source image is not readable and could not be made readable.");
                return;
            }

            List<BlockColor> generatedCells;
            try
            {
                generatedCells = GenerateCells(
                    sourceImage,
                    Mathf.Clamp01(alphaThreshold),
                    backgroundToNone,
                    backgroundSampleMode,
                    Mathf.Clamp01(backgroundTolerance),
                    brightnessToNone,
                    Mathf.Clamp01(darkToNoneThreshold),
                    Mathf.Clamp01(lightToNoneThreshold),
                    Mathf.Clamp(noiseCleanupPasses, 0, 4),
                    Mathf.Clamp(minimumNeighborCount, 1, 4));
            }
            catch (UnityException)
            {
                SetFailure("Source image is not readable and could not be made readable.");
                return;
            }

            if (generatedCells.Count != ExpectedCellCount)
            {
                SetFailure($"Generated cells count must be {ExpectedCellCount}, but was {generatedCells.Count}.");
                return;
            }

            LargePatternVisualConfig config = LoadOrCreateOutputAsset(outputPath);
            if (config == null)
            {
                return;
            }

            WriteSerializedFields(config, generatedCells);

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            int noneCellCount = CountCells(generatedCells, BlockColor.None);
            int nonNoneCellCount = generatedCells.Count - noneCellCount;
            string successMessage = $"Generated visual config: {outputPath} Width={OutputWidth} Height={OutputHeight} Cells={generatedCells.Count} NonNoneCells={nonNoneCellCount} NoneCells={noneCellCount}";
            SetStatus(successMessage, MessageType.Info);
            Debug.Log($"{SuccessLogPrefix} {outputPath} Width={OutputWidth} Height={OutputHeight} Cells={generatedCells.Count} NonNoneCells={nonNoneCellCount} NoneCells={noneCellCount}");
        }

        private bool ValidateInputs()
        {
            if (sourceImage == null)
            {
                SetFailure("Select a Texture2D source image before generating.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(outputPath) || !outputPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                SetFailure("Output path must start with Assets/.");
                return false;
            }

            if (!outputPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
            {
                SetFailure("Output path must end with .asset.");
                return false;
            }

            return true;
        }

        private static bool EnsureTextureReadable(ref Texture2D texture)
        {
            if (CanReadTexture(texture))
            {
                return true;
            }

            string assetPath = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return false;
            }

            if (!importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            Texture2D reloadedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (CanReadTexture(reloadedTexture))
            {
                texture = reloadedTexture;
                return true;
            }

            return false;
        }

        private static bool CanReadTexture(Texture2D texture)
        {
            if (texture == null)
            {
                return false;
            }

            try
            {
                texture.GetPixel(0, 0);
                return true;
            }
            catch (UnityException)
            {
                return false;
            }
        }

        private static List<BlockColor> GenerateCells(
            Texture2D texture,
            float alphaCutoff,
            bool removeBackground,
            BackgroundSampleMode sampleMode,
            float backgroundDistanceTolerance,
            bool removeByBrightness,
            float darkThreshold,
            float lightThreshold,
            int cleanupPasses,
            int minimumSameColorNeighborCount)
        {
            Color[] sampledColors = SampleCroppedTexture(texture);
            Color backgroundColor = GetBackgroundColor(sampledColors, sampleMode);
            float backgroundToleranceSquared = backgroundDistanceTolerance * backgroundDistanceTolerance;

            List<BlockColor> cells = new List<BlockColor>(ExpectedCellCount);
            for (int i = 0; i < sampledColors.Length; i++)
            {
                Color sampledColor = sampledColors[i];
                cells.Add(ShouldMapToNone(
                    sampledColor,
                    alphaCutoff,
                    removeBackground,
                    backgroundColor,
                    backgroundToleranceSquared,
                    removeByBrightness,
                    darkThreshold,
                    lightThreshold)
                    ? BlockColor.None
                    : GetNearestBlockColor(sampledColor));
            }

            ApplyNoiseCleanup(cells, cleanupPasses, minimumSameColorNeighborCount);
            return cells;
        }

        private static Color[] SampleCroppedTexture(Texture2D texture)
        {
            Color[] sampledColors = new Color[ExpectedCellCount];
            Rect cropRect = GetCenteredCropRect(texture.width, texture.height, (float)OutputWidth / OutputHeight);

            for (int y = 0; y < OutputHeight; y++)
            {
                for (int x = 0; x < OutputWidth; x++)
                {
                    float pixelX = cropRect.xMin + ((x + 0.5f) / OutputWidth * cropRect.width);
                    float pixelY = cropRect.yMin + ((y + 0.5f) / OutputHeight * cropRect.height);
                    float u = Mathf.Clamp01(pixelX / texture.width);
                    float v = Mathf.Clamp01(pixelY / texture.height);
                    sampledColors[(y * OutputWidth) + x] = texture.GetPixelBilinear(u, v);
                }
            }

            return sampledColors;
        }

        private static Color GetBackgroundColor(Color[] sampledColors, BackgroundSampleMode sampleMode)
        {
            if (sampleMode == BackgroundSampleMode.FourCorners)
            {
                Color topLeft = sampledColors[(OutputHeight - 1) * OutputWidth];
                Color topRight = sampledColors[ExpectedCellCount - 1];
                Color bottomLeft = sampledColors[0];
                Color bottomRight = sampledColors[OutputWidth - 1];
                return (topLeft + topRight + bottomLeft + bottomRight) * 0.25f;
            }

            return sampledColors[(OutputHeight - 1) * OutputWidth];
        }

        private static bool ShouldMapToNone(
            Color color,
            float alphaCutoff,
            bool removeBackground,
            Color backgroundColor,
            float backgroundToleranceSquared,
            bool removeByBrightness,
            float darkThreshold,
            float lightThreshold)
        {
            if (color.a < alphaCutoff)
            {
                return true;
            }

            if (removeBackground && GetRgbDistanceSquared(color, backgroundColor) < backgroundToleranceSquared)
            {
                return true;
            }

            if (!removeByBrightness)
            {
                return false;
            }

            float brightness = (color.r + color.g + color.b) / 3f;
            return brightness < darkThreshold || brightness > lightThreshold;
        }

        private static void ApplyNoiseCleanup(List<BlockColor> cells, int passes, int minimumSameColorNeighborCount)
        {
            for (int pass = 0; pass < passes; pass++)
            {
                BlockColor[] previousCells = cells.ToArray();
                for (int y = 0; y < OutputHeight; y++)
                {
                    for (int x = 0; x < OutputWidth; x++)
                    {
                        int index = (y * OutputWidth) + x;
                        BlockColor currentColor = previousCells[index];
                        if (currentColor == BlockColor.None)
                        {
                            continue;
                        }

                        int sameColorNeighborCount = CountSameColorNeighbors(previousCells, x, y, currentColor);
                        if (sameColorNeighborCount >= minimumSameColorNeighborCount)
                        {
                            continue;
                        }

                        cells[index] = GetMostCommonNonNoneNeighborColor(previousCells, x, y);
                    }
                }
            }
        }

        private static int CountSameColorNeighbors(BlockColor[] cells, int x, int y, BlockColor color)
        {
            int count = 0;
            foreach (BlockColor neighborColor in GetNeighborColors(cells, x, y))
            {
                if (neighborColor == color)
                {
                    count++;
                }
            }

            return count;
        }

        private static BlockColor GetMostCommonNonNoneNeighborColor(BlockColor[] cells, int x, int y)
        {
            int bestCount = 0;
            BlockColor bestColor = BlockColor.None;
            foreach (ColorMapping mapping in ColorMappings)
            {
                int count = 0;
                foreach (BlockColor neighborColor in GetNeighborColors(cells, x, y))
                {
                    if (neighborColor == mapping.BlockColor)
                    {
                        count++;
                    }
                }

                if (count > bestCount)
                {
                    bestCount = count;
                    bestColor = mapping.BlockColor;
                }
            }

            return bestColor;
        }

        private static IEnumerable<BlockColor> GetNeighborColors(BlockColor[] cells, int x, int y)
        {
            if (y > 0)
            {
                yield return cells[((y - 1) * OutputWidth) + x];
            }

            if (y < OutputHeight - 1)
            {
                yield return cells[((y + 1) * OutputWidth) + x];
            }

            if (x > 0)
            {
                yield return cells[(y * OutputWidth) + x - 1];
            }

            if (x < OutputWidth - 1)
            {
                yield return cells[(y * OutputWidth) + x + 1];
            }
        }

        private static int CountCells(List<BlockColor> cells, BlockColor color)
        {
            int count = 0;
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i] == color)
                {
                    count++;
                }
            }

            return count;
        }

        private static Rect GetCenteredCropRect(int textureWidth, int textureHeight, float targetAspect)
        {
            float sourceAspect = (float)textureWidth / textureHeight;
            float cropWidth = textureWidth;
            float cropHeight = textureHeight;

            if (sourceAspect > targetAspect)
            {
                cropWidth = textureHeight * targetAspect;
            }
            else if (sourceAspect < targetAspect)
            {
                cropHeight = textureWidth / targetAspect;
            }

            float cropX = (textureWidth - cropWidth) * 0.5f;
            float cropY = (textureHeight - cropHeight) * 0.5f;
            return new Rect(cropX, cropY, cropWidth, cropHeight);
        }

        private static BlockColor GetNearestBlockColor(Color color)
        {
            BlockColor nearestColor = ColorMappings[0].BlockColor;
            float nearestDistance = float.MaxValue;

            foreach (ColorMapping mapping in ColorMappings)
            {
                float distance = GetRgbDistanceSquared(color, mapping.Color);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestColor = mapping.BlockColor;
                }
            }

            return nearestColor;
        }

        private static float GetRgbDistanceSquared(Color a, Color b)
        {
            float red = a.r - b.r;
            float green = a.g - b.g;
            float blue = a.b - b.b;
            return (red * red) + (green * green) + (blue * blue);
        }

        private LargePatternVisualConfig LoadOrCreateOutputAsset(string assetPath)
        {
            LargePatternVisualConfig config = AssetDatabase.LoadAssetAtPath<LargePatternVisualConfig>(assetPath);
            if (config != null)
            {
                return config;
            }

            UnityEngine.Object existingAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (existingAsset != null)
            {
                SetFailure("Output asset exists but is not a LargePatternVisualConfig.");
                return null;
            }

            EnsureOutputDirectoryExists(assetPath);

            config = CreateInstance<LargePatternVisualConfig>();
            AssetDatabase.CreateAsset(config, assetPath);
            return config;
        }

        private static void EnsureOutputDirectoryExists(string assetPath)
        {
            string directory = Path.GetDirectoryName(assetPath);
            if (string.IsNullOrEmpty(directory) || AssetDatabase.IsValidFolder(directory))
            {
                return;
            }

            Directory.CreateDirectory(directory);
            AssetDatabase.Refresh();
        }

        private void WriteSerializedFields(LargePatternVisualConfig config, List<BlockColor> generatedCells)
        {
            SerializedObject serializedObject = new SerializedObject(config);
            serializedObject.FindProperty("width").intValue = OutputWidth;
            serializedObject.FindProperty("height").intValue = OutputHeight;
            serializedObject.FindProperty("cellSize").floatValue = Mathf.Max(0.01f, cellSize);

            SerializedProperty cellsProperty = serializedObject.FindProperty("cells");
            cellsProperty.arraySize = generatedCells.Count;
            for (int i = 0; i < generatedCells.Count; i++)
            {
                cellsProperty.GetArrayElementAtIndex(i).enumValueIndex = (int)generatedCells[i];
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void SetFailure(string message)
        {
            SetStatus(message, MessageType.Error);
            Debug.LogError(message);
        }

        private void SetStatus(string message, MessageType messageType)
        {
            statusMessage = message;
            statusType = messageType;
        }

        private enum BackgroundSampleMode
        {
            TopLeft,
            FourCorners
        }

        private readonly struct ColorMapping
        {
            public ColorMapping(BlockColor blockColor, Color color)
            {
                BlockColor = blockColor;
                Color = color;
            }

            public BlockColor BlockColor { get; }
            public Color Color { get; }
        }
    }
}
