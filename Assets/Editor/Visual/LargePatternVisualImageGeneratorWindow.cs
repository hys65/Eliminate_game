using System;
using System.Collections.Generic;
using System.IO;
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
        private const float DefaultBackgroundThreshold = 0.15f;
        private const float DefaultAlphaThreshold = 0.1f;
        private const string DefaultOutputPath = "Assets/GameConfigs/Visual/LargePatternVisual_30x28_FromImage.asset";
        private const string SuccessLogPrefix = "[LargePatternVisualImageGenerator] Generated visual config:";

        private enum PalettePreset
        {
            VisualPalette_Default16
        }

        private enum BackgroundSampleMode
        {
            FourCorners
        }

        [SerializeField] private Texture2D sourceImage;
        [SerializeField] private string outputPath = DefaultOutputPath;
        [SerializeField] private float cellSize = DefaultCellSize;
        [SerializeField] private PalettePreset palettePreset = PalettePreset.VisualPalette_Default16;
        [SerializeField] private bool backgroundToNone = true;
        [SerializeField] private BackgroundSampleMode backgroundSampleMode = BackgroundSampleMode.FourCorners;
        [SerializeField] private float backgroundThreshold = DefaultBackgroundThreshold;
        [SerializeField] private float alphaThreshold = DefaultAlphaThreshold;
        [SerializeField] private string statusMessage = string.Empty;
        [SerializeField] private MessageType statusType = MessageType.None;

        [MenuItem("Tools/Eliminate Game/Visual/Generate Large Pattern From Image")]
        public static void ShowWindow()
        {
            LargePatternVisualImageGeneratorWindow window = GetWindow<LargePatternVisualImageGeneratorWindow>("Large Pattern From Image");
            window.minSize = new Vector2(460f, 260f);
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
            EditorGUILayout.LabelField("Image-to-LargePatternVisualConfig Palette 1.0", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Visual-only generator. Writes 30 x 28 palette indices only; it does not write gameplay Pattern data.", MessageType.Info);
            EditorGUILayout.Space();

            sourceImage = (Texture2D)EditorGUILayout.ObjectField("Source Image", sourceImage, typeof(Texture2D), false);
            outputPath = EditorGUILayout.TextField("Output Path", outputPath);
            cellSize = EditorGUILayout.FloatField("Cell Size", cellSize);
            palettePreset = (PalettePreset)EditorGUILayout.EnumPopup("Palette Preset", palettePreset);
            backgroundToNone = EditorGUILayout.Toggle("Background To None", backgroundToNone);

            using (new EditorGUI.DisabledScope(!backgroundToNone))
            {
                backgroundSampleMode = (BackgroundSampleMode)EditorGUILayout.EnumPopup("Background Sample Mode", backgroundSampleMode);
                backgroundThreshold = EditorGUILayout.Slider("Background Threshold", backgroundThreshold, 0f, 1f);
            }

            alphaThreshold = EditorGUILayout.Slider("Alpha Threshold", alphaThreshold, 0f, 1f);
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

            List<Color> paletteColors = GetPaletteColors(palettePreset);
            Color[] sampledColors;
            try
            {
                sampledColors = SampleCroppedTexture(sourceImage);
            }
            catch (UnityException)
            {
                SetFailure("Source image is not readable and could not be made readable.");
                return;
            }

            List<int> cellPaletteIndices = GeneratePaletteIndices(
                sampledColors,
                paletteColors,
                backgroundToNone,
                backgroundSampleMode,
                Mathf.Clamp01(backgroundThreshold),
                Mathf.Clamp01(alphaThreshold));

            if (cellPaletteIndices.Count != ExpectedCellCount)
            {
                SetFailure($"Generated cells count must be {ExpectedCellCount}, but was {cellPaletteIndices.Count}.");
                return;
            }

            LargePatternVisualConfig config = LoadOrCreateOutputAsset(outputPath);
            if (config == null)
            {
                return;
            }

            config.SetPaletteVisualData(OutputWidth, OutputHeight, Mathf.Max(0.01f, cellSize), paletteColors, cellPaletteIndices);

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            int noneCellCount = CountCells(cellPaletteIndices, LargePatternVisualConfig.TransparentPaletteIndex);
            int nonNoneCellCount = cellPaletteIndices.Count - noneCellCount;
            string successMessage = $"Generated visual config: {outputPath} Width={OutputWidth} Height={OutputHeight} Cells={cellPaletteIndices.Count} PaletteSize={paletteColors.Count} NonNoneCells={nonNoneCellCount} NoneCells={noneCellCount}";
            SetStatus(successMessage, MessageType.Info);
            Debug.Log($"{SuccessLogPrefix} {outputPath} Width={OutputWidth} Height={OutputHeight} Cells={cellPaletteIndices.Count} PaletteSize={paletteColors.Count} NonNoneCells={nonNoneCellCount} NoneCells={noneCellCount}");
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

        private static List<Color> GetPaletteColors(PalettePreset preset)
        {
            switch (preset)
            {
                case PalettePreset.VisualPalette_Default16:
                default:
                    return LargePatternVisualConfig.CreateDefaultPaletteColors();
            }
        }

        private static List<int> GeneratePaletteIndices(
            Color[] sampledColors,
            List<Color> paletteColors,
            bool removeBackground,
            BackgroundSampleMode sampleMode,
            float backgroundCutoff,
            float alphaCutoff)
        {
            List<int> indices = new List<int>(ExpectedCellCount);
            Color backgroundColor = GetBackgroundColor(sampledColors, sampleMode);
            float backgroundCutoffSquared = backgroundCutoff * backgroundCutoff;

            for (int i = 0; i < sampledColors.Length; i++)
            {
                Color sourceColor = sampledColors[i];
                if (sourceColor.a < alphaCutoff
                    || (removeBackground && GetRgbDistanceSquared(sourceColor, backgroundColor) < backgroundCutoffSquared))
                {
                    indices.Add(LargePatternVisualConfig.TransparentPaletteIndex);
                    continue;
                }

                indices.Add(GetNearestVisiblePaletteIndex(sourceColor, paletteColors));
            }

            return indices;
        }

        private static int GetNearestVisiblePaletteIndex(Color color, List<Color> paletteColors)
        {
            int nearestIndex = paletteColors.Count > 1 ? 1 : LargePatternVisualConfig.TransparentPaletteIndex;
            float nearestDistance = float.MaxValue;
            for (int i = 1; i < paletteColors.Count; i++)
            {
                Color paletteColor = paletteColors[i];
                if (paletteColor.a <= 0.001f)
                {
                    continue;
                }

                float distance = GetRgbDistanceSquared(color, paletteColor);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestIndex = i;
                }
            }

            return nearestIndex;
        }

        private static Color[] SampleCroppedTexture(Texture2D texture)
        {
            Color[] sampledColors = new Color[ExpectedCellCount];
            Rect cropRect = GetCenteredCropRect(texture.width, texture.height, (float)OutputWidth / OutputHeight);

            for (int y = 0; y < OutputHeight; y++)
            {
                for (int x = 0; x < OutputWidth; x++)
                {
                    float pixelX = cropRect.xMin + (((x + 0.5f) / OutputWidth) * cropRect.width);
                    float pixelY = cropRect.yMin + (((y + 0.5f) / OutputHeight) * cropRect.height);
                    float u = Mathf.Clamp01(pixelX / texture.width);
                    float v = Mathf.Clamp01(pixelY / texture.height);
                    sampledColors[(y * OutputWidth) + x] = texture.GetPixelBilinear(u, v);
                }
            }

            return sampledColors;
        }

        private static Rect GetCenteredCropRect(int sourceWidth, int sourceHeight, float targetAspect)
        {
            float sourceAspect = (float)sourceWidth / sourceHeight;
            if (sourceAspect > targetAspect)
            {
                float cropWidth = sourceHeight * targetAspect;
                float xMin = (sourceWidth - cropWidth) * 0.5f;
                return new Rect(xMin, 0f, cropWidth, sourceHeight);
            }

            float cropHeight = sourceWidth / targetAspect;
            float yMin = (sourceHeight - cropHeight) * 0.5f;
            return new Rect(0f, yMin, sourceWidth, cropHeight);
        }

        private static Color GetBackgroundColor(Color[] sampledColors, BackgroundSampleMode sampleMode)
        {
            switch (sampleMode)
            {
                case BackgroundSampleMode.FourCorners:
                default:
                    Color topLeft = sampledColors[(OutputHeight - 1) * OutputWidth];
                    Color topRight = sampledColors[ExpectedCellCount - 1];
                    Color bottomLeft = sampledColors[0];
                    Color bottomRight = sampledColors[OutputWidth - 1];
                    return (topLeft + topRight + bottomLeft + bottomRight) * 0.25f;
            }
        }

        private static float GetRgbDistanceSquared(Color left, Color right)
        {
            float deltaR = left.r - right.r;
            float deltaG = left.g - right.g;
            float deltaB = left.b - right.b;
            return (deltaR * deltaR) + (deltaG * deltaG) + (deltaB * deltaB);
        }

        private static int CountCells(List<int> cells, int targetIndex)
        {
            int count = 0;
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i] == targetIndex)
                {
                    count++;
                }
            }

            return count;
        }

        private LargePatternVisualConfig LoadOrCreateOutputAsset(string assetPath)
        {
            string directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }

            LargePatternVisualConfig config = AssetDatabase.LoadAssetAtPath<LargePatternVisualConfig>(assetPath);
            if (config != null)
            {
                return config;
            }

            config = CreateInstance<LargePatternVisualConfig>();
            AssetDatabase.CreateAsset(config, assetPath);
            return config;
        }

        private void SetFailure(string message)
        {
            SetStatus(message, MessageType.Error);
            Debug.LogError($"[LargePatternVisualImageGenerator] {message}");
        }

        private void SetStatus(string message, MessageType messageType)
        {
            statusMessage = message;
            statusType = messageType;
        }
    }
}
