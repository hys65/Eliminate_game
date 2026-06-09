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
        private const float DefaultBackgroundTolerance = 0.12f;
        private const float DefaultDarkToNoneThreshold = 0.12f;
        private const float DefaultLightToNoneThreshold = 0.92f;
        private const int DefaultNoiseCleanupPasses = 1;
        private const int DefaultMinimumNeighborCount = 1;
        private const string DefaultOutputPath = "Assets/GameConfigs/Visual/LargePatternVisual_30x28_FromImage.asset";
        private const float DefaultEdgeStrengthThreshold = 0.18f;
        private const bool DefaultSilhouetteFirstMode = true;
        private const float DefaultForegroundThreshold = 0.16f;
        private const bool DefaultUseLargestConnectedComponent = true;
        private const int DefaultMaskClosePasses = 2;
        private const int DefaultMaskOpenPasses = 1;
        private const int DefaultInteriorSimplifyPasses = 2;
        private const int DefaultKeepHoleMinArea = 6;
        private const int DefaultMaxPaletteColors = 3;
        private const BlockColorChoice DefaultPreferredFillColor = BlockColorChoice.Purple;
        private const BlockColorChoice DefaultPreferredAccentColor = BlockColorChoice.Yellow;
        private const BlockColorChoice DefaultPreferredDetailColor = BlockColorChoice.Green;
        private const bool DefaultOutlineEnabled = true;
        private const BlockColorChoice DefaultOutlineColor = BlockColorChoice.Purple;
        private const int DefaultOutlineThickness = 1;
        private const bool DefaultApplyInteriorFloodFillBias = true;
        private const OutlineBlockColor DefaultLegacyOutlineColor = OutlineBlockColor.Purple;
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
        [SerializeField] private bool silhouetteFirstMode = DefaultSilhouetteFirstMode;
        [SerializeField] private float foregroundThreshold = DefaultForegroundThreshold;
        [SerializeField] private bool useLargestConnectedComponent = DefaultUseLargestConnectedComponent;
        [SerializeField] private int maskClosePasses = DefaultMaskClosePasses;
        [SerializeField] private int maskOpenPasses = DefaultMaskOpenPasses;
        [SerializeField] private int interiorSimplifyPasses = DefaultInteriorSimplifyPasses;
        [SerializeField] private int keepHoleMinArea = DefaultKeepHoleMinArea;
        [SerializeField] private int maxPaletteColors = DefaultMaxPaletteColors;
        [SerializeField] private BlockColorChoice preferredFillColor = DefaultPreferredFillColor;
        [SerializeField] private BlockColorChoice preferredAccentColor = DefaultPreferredAccentColor;
        [SerializeField] private BlockColorChoice preferredDetailColor = DefaultPreferredDetailColor;
        [SerializeField] private bool outlineEnabled = DefaultOutlineEnabled;
        [SerializeField] private BlockColorChoice silhouetteOutlineColor = DefaultOutlineColor;
        [SerializeField] private int outlineThickness = DefaultOutlineThickness;
        [SerializeField] private bool applyInteriorFloodFillBias = DefaultApplyInteriorFloodFillBias;
        [SerializeField] private bool preserveEdges = true;
        [SerializeField] private float edgeStrengthThreshold = DefaultEdgeStrengthThreshold;
        [SerializeField] private OutlineBlockColor outlineColor = DefaultLegacyOutlineColor;
        [SerializeField] private bool applyOutlineAfterCleanup = true;
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
            EditorGUILayout.LabelField("Image-to-LargePatternVisualConfig Pipeline 2.0", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            sourceImage = (Texture2D)EditorGUILayout.ObjectField("Source Image", sourceImage, typeof(Texture2D), false);
            outputPath = EditorGUILayout.TextField("Output Path", outputPath);
            cellSize = EditorGUILayout.FloatField("Cell Size", cellSize);
            alphaThreshold = EditorGUILayout.Slider("Alpha Threshold", alphaThreshold, 0f, 1f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Silhouette First Mode", EditorStyles.boldLabel);
            silhouetteFirstMode = EditorGUILayout.Toggle("Silhouette First Mode", silhouetteFirstMode);
            using (new EditorGUI.DisabledScope(!silhouetteFirstMode))
            {
                foregroundThreshold = EditorGUILayout.Slider("Foreground Threshold", foregroundThreshold, 0f, 1f);
                useLargestConnectedComponent = EditorGUILayout.Toggle("Use Largest Connected Component", useLargestConnectedComponent);
                maskClosePasses = EditorGUILayout.IntSlider("Mask Close Passes", maskClosePasses, 0, 4);
                maskOpenPasses = EditorGUILayout.IntSlider("Mask Open Passes", maskOpenPasses, 0, 3);
                interiorSimplifyPasses = EditorGUILayout.IntSlider("Interior Simplify Passes", interiorSimplifyPasses, 0, 4);
                keepHoleMinArea = EditorGUILayout.IntSlider("Keep Hole Min Area", keepHoleMinArea, 0, 50);
                maxPaletteColors = EditorGUILayout.IntSlider("Max Palette Colors", maxPaletteColors, 1, 5);
                preferredFillColor = (BlockColorChoice)EditorGUILayout.EnumPopup("Preferred Fill Color", preferredFillColor);
                preferredAccentColor = (BlockColorChoice)EditorGUILayout.EnumPopup("Preferred Accent Color", preferredAccentColor);
                preferredDetailColor = (BlockColorChoice)EditorGUILayout.EnumPopup("Preferred Detail Color", preferredDetailColor);
                outlineEnabled = EditorGUILayout.Toggle("Outline Enabled", outlineEnabled);
                using (new EditorGUI.DisabledScope(!outlineEnabled))
                {
                    silhouetteOutlineColor = (BlockColorChoice)EditorGUILayout.EnumPopup("Outline Color", silhouetteOutlineColor);
                    outlineThickness = EditorGUILayout.IntSlider("Outline Thickness", outlineThickness, 1, 1);
                }
                applyInteriorFloodFillBias = EditorGUILayout.Toggle("Apply Interior Flood Fill Bias", applyInteriorFloodFillBias);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Legacy 1.2 Cleanup", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(silhouetteFirstMode))
            {
                backgroundToNone = EditorGUILayout.Toggle("Background To None", backgroundToNone);
                using (new EditorGUI.DisabledScope(!backgroundToNone || silhouetteFirstMode))
                {
                    backgroundSampleMode = (BackgroundSampleMode)EditorGUILayout.EnumPopup("Background Sample Mode", backgroundSampleMode);
                    backgroundTolerance = EditorGUILayout.Slider("Background Tolerance", backgroundTolerance, 0f, 1f);
                }

                brightnessToNone = EditorGUILayout.Toggle("Brightness To None", brightnessToNone);
                using (new EditorGUI.DisabledScope(!brightnessToNone || silhouetteFirstMode))
                {
                    darkToNoneThreshold = EditorGUILayout.Slider("Dark To None Threshold", darkToNoneThreshold, 0f, 1f);
                    lightToNoneThreshold = EditorGUILayout.Slider("Light To None Threshold", lightToNoneThreshold, 0f, 1f);
                }

                noiseCleanupPasses = EditorGUILayout.IntSlider("Noise Cleanup Passes", noiseCleanupPasses, 0, 4);
                minimumNeighborCount = EditorGUILayout.IntSlider("Minimum Neighbor Count", minimumNeighborCount, 1, 4);
                preserveEdges = EditorGUILayout.Toggle("Preserve Edges", preserveEdges);
                using (new EditorGUI.DisabledScope(!preserveEdges || silhouetteFirstMode))
                {
                    edgeStrengthThreshold = EditorGUILayout.Slider("Edge Strength Threshold", edgeStrengthThreshold, 0f, 1f);
                    outlineColor = (OutlineBlockColor)EditorGUILayout.EnumPopup("Outline Color", outlineColor);
                    applyOutlineAfterCleanup = EditorGUILayout.Toggle("Apply Outline After Cleanup", applyOutlineAfterCleanup);
                }
            }

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

            GeneratedCellsResult generatedResult;
            try
            {
                generatedResult = silhouetteFirstMode
                    ? GenerateSilhouetteFirstCells(
                        sourceImage,
                        Mathf.Clamp01(alphaThreshold),
                        Mathf.Clamp01(foregroundThreshold),
                        useLargestConnectedComponent,
                        Mathf.Clamp(maskClosePasses, 0, 4),
                        Mathf.Clamp(maskOpenPasses, 0, 3),
                        Mathf.Clamp(interiorSimplifyPasses, 0, 4),
                        Mathf.Clamp(keepHoleMinArea, 0, 50),
                        Mathf.Clamp(maxPaletteColors, 1, 5),
                        GetBlockColor(preferredFillColor),
                        GetBlockColor(preferredAccentColor),
                        GetBlockColor(preferredDetailColor),
                        outlineEnabled,
                        GetBlockColor(silhouetteOutlineColor),
                        DefaultOutlineThickness,
                        applyInteriorFloodFillBias)
                    : GenerateCells(
                        sourceImage,
                        Mathf.Clamp01(alphaThreshold),
                        backgroundToNone,
                        backgroundSampleMode,
                        Mathf.Clamp01(backgroundTolerance),
                        brightnessToNone,
                        Mathf.Clamp01(darkToNoneThreshold),
                        Mathf.Clamp01(lightToNoneThreshold),
                        Mathf.Clamp(noiseCleanupPasses, 0, 4),
                        Mathf.Clamp(minimumNeighborCount, 1, 4),
                        preserveEdges,
                        Mathf.Clamp01(edgeStrengthThreshold),
                        GetBlockColor(outlineColor),
                        applyOutlineAfterCleanup);
            }
            catch (UnityException)
            {
                SetFailure("Source image is not readable and could not be made readable.");
                return;
            }

            List<BlockColor> generatedCells = generatedResult.Cells;
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
            string successMessage = $"Generated visual config: {outputPath} Width={OutputWidth} Height={OutputHeight} Cells={generatedCells.Count} NonNoneCells={nonNoneCellCount} NoneCells={noneCellCount} ActivePalette={generatedResult.ActivePaletteLog}";
            SetStatus(successMessage, MessageType.Info);
            Debug.Log($"{SuccessLogPrefix} {outputPath} Width={OutputWidth} Height={OutputHeight} Cells={generatedCells.Count} NonNoneCells={nonNoneCellCount} NoneCells={noneCellCount} ActivePalette={generatedResult.ActivePaletteLog}");
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

        private static GeneratedCellsResult GenerateCells(
            Texture2D texture,
            float alphaCutoff,
            bool removeBackground,
            BackgroundSampleMode sampleMode,
            float backgroundDistanceTolerance,
            bool removeByBrightness,
            float darkThreshold,
            float lightThreshold,
            int cleanupPasses,
            int minimumSameColorNeighborCount,
            bool preserveEdges,
            float edgeStrengthCutoff,
            BlockColor outlineBlockColor,
            bool applyOutline)
        {
            Color[] sampledColors = SampleCroppedTexture(texture);
            bool[] edgeMap = BuildEdgeMap(sampledColors, edgeStrengthCutoff);
            int edgeCellCount = CountEdgeCells(edgeMap);
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
                    lightThreshold,
                    preserveEdges && edgeMap[i])
                    ? BlockColor.None
                    : GetNearestBlockColor(sampledColor));
            }

            ApplyNoiseCleanup(cells, cleanupPasses, minimumSameColorNeighborCount, preserveEdges ? edgeMap : null);
            if (preserveEdges && applyOutline)
            {
                ApplyOutlineAfterCleanup(cells, edgeMap, outlineBlockColor);
            }

            return new GeneratedCellsResult(cells, edgeCellCount);
        }

        private static GeneratedCellsResult GenerateSilhouetteFirstCells(
            Texture2D texture,
            float alphaCutoff,
            float foregroundDistanceThreshold,
            bool keepLargestComponentOnly,
            int closePasses,
            int openPasses,
            int simplifyPasses,
            int keepHoleMinArea,
            int maxActivePaletteColors,
            BlockColor preferredFillColor,
            BlockColor preferredAccentColor,
            BlockColor preferredDetailColor,
            bool enableOutline,
            BlockColor outlineBlockColor,
            int outlineThickness,
            bool biasInteriorFloodFill)
        {
            Color[] sampledColors = SampleCroppedTexture(texture);
            Color backgroundColor = GetBackgroundColor(sampledColors, BackgroundSampleMode.FourCorners);
            bool[] foregroundMask = BuildForegroundMask(sampledColors, backgroundColor, foregroundDistanceThreshold, alphaCutoff);

            if (keepLargestComponentOnly)
            {
                foregroundMask = KeepLargestConnectedComponent(foregroundMask);
            }

            for (int pass = 0; pass < closePasses; pass++)
            {
                foregroundMask = ErodeMask(DilateMask(foregroundMask));
            }

            for (int pass = 0; pass < openPasses; pass++)
            {
                foregroundMask = DilateMask(ErodeMask(foregroundMask));
            }

            FillSmallInternalHoles(foregroundMask, keepHoleMinArea);

            List<BlockColor> activePalette = BuildActivePalette(sampledColors, foregroundMask, maxActivePaletteColors, preferredFillColor);
            BlockColor[] cells = new BlockColor[ExpectedCellCount];
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = foregroundMask[i] ? GetNearestBlockColor(sampledColors[i], activePalette) : BlockColor.None;
            }

            SimplifyInteriorColors(cells, foregroundMask, simplifyPasses);
            if (biasInteriorFloodFill)
            {
                ApplyInteriorFloodFillBias(cells, foregroundMask, activePalette, preferredFillColor, preferredAccentColor, preferredDetailColor);
            }

            int outlineCellCount = 0;
            if (enableOutline && outlineThickness == 1)
            {
                outlineCellCount = ApplySilhouetteOutline(cells, foregroundMask, outlineBlockColor);
            }

            for (int i = 0; i < cells.Length; i++)
            {
                if (!foregroundMask[i])
                {
                    cells[i] = BlockColor.None;
                }
            }

            return new GeneratedCellsResult(new List<BlockColor>(cells), outlineCellCount, BuildPaletteLog(activePalette));
        }

        private static bool[] BuildForegroundMask(Color[] sampledColors, Color backgroundColor, float threshold, float alphaCutoff)
        {
            float thresholdSquared = threshold * threshold;
            bool[] mask = new bool[ExpectedCellCount];
            for (int i = 0; i < sampledColors.Length; i++)
            {
                mask[i] = sampledColors[i].a >= alphaCutoff && GetRgbDistanceSquared(sampledColors[i], backgroundColor) >= thresholdSquared;
            }

            return mask;
        }

        private static bool[] KeepLargestConnectedComponent(bool[] mask)
        {
            bool[] visited = new bool[ExpectedCellCount];
            bool[] largest = new bool[ExpectedCellCount];
            int largestCount = 0;
            Queue<int> queue = new Queue<int>();
            List<int> component = new List<int>();

            for (int i = 0; i < ExpectedCellCount; i++)
            {
                if (!mask[i] || visited[i])
                {
                    continue;
                }

                component.Clear();
                visited[i] = true;
                queue.Enqueue(i);
                while (queue.Count > 0)
                {
                    int current = queue.Dequeue();
                    component.Add(current);
                    foreach (int neighbor in GetNeighborIndexes(current))
                    {
                        if (!mask[neighbor] || visited[neighbor])
                        {
                            continue;
                        }

                        visited[neighbor] = true;
                        queue.Enqueue(neighbor);
                    }
                }

                if (component.Count <= largestCount)
                {
                    continue;
                }

                Array.Clear(largest, 0, largest.Length);
                for (int c = 0; c < component.Count; c++)
                {
                    largest[component[c]] = true;
                }

                largestCount = component.Count;
            }

            return largest;
        }

        private static bool[] DilateMask(bool[] mask)
        {
            bool[] result = new bool[ExpectedCellCount];
            for (int i = 0; i < ExpectedCellCount; i++)
            {
                if (mask[i])
                {
                    result[i] = true;
                    continue;
                }

                foreach (int neighbor in GetNeighborIndexes(i))
                {
                    if (mask[neighbor])
                    {
                        result[i] = true;
                        break;
                    }
                }
            }

            return result;
        }

        private static bool[] ErodeMask(bool[] mask)
        {
            bool[] result = new bool[ExpectedCellCount];
            for (int y = 0; y < OutputHeight; y++)
            {
                for (int x = 0; x < OutputWidth; x++)
                {
                    int index = (y * OutputWidth) + x;
                    if (!mask[index])
                    {
                        continue;
                    }

                    bool keep = true;
                    foreach (int neighbor in GetNeighborIndexes(index))
                    {
                        if (!mask[neighbor])
                        {
                            keep = false;
                            break;
                        }
                    }

                    result[index] = keep;
                }
            }

            return result;
        }

        private static void FillSmallInternalHoles(bool[] foregroundMask, int keepHoleMinArea)
        {
            bool[] visited = new bool[ExpectedCellCount];
            Queue<int> queue = new Queue<int>();
            List<int> component = new List<int>();

            for (int i = 0; i < ExpectedCellCount; i++)
            {
                if (foregroundMask[i] || visited[i])
                {
                    continue;
                }

                bool touchesBoundary = false;
                component.Clear();
                visited[i] = true;
                queue.Enqueue(i);
                while (queue.Count > 0)
                {
                    int current = queue.Dequeue();
                    component.Add(current);
                    int x = current % OutputWidth;
                    int y = current / OutputWidth;
                    touchesBoundary |= x == 0 || x == OutputWidth - 1 || y == 0 || y == OutputHeight - 1;

                    foreach (int neighbor in GetNeighborIndexes(current))
                    {
                        if (foregroundMask[neighbor] || visited[neighbor])
                        {
                            continue;
                        }

                        visited[neighbor] = true;
                        queue.Enqueue(neighbor);
                    }
                }

                if (touchesBoundary || component.Count >= keepHoleMinArea)
                {
                    continue;
                }

                for (int c = 0; c < component.Count; c++)
                {
                    foregroundMask[component[c]] = true;
                }
            }
        }

        private static List<BlockColor> BuildActivePalette(Color[] sampledColors, bool[] foregroundMask, int maxActivePaletteColors, BlockColor preferredFillColor)
        {
            Dictionary<BlockColor, int> counts = new Dictionary<BlockColor, int>();
            foreach (ColorMapping mapping in ColorMappings)
            {
                counts[mapping.BlockColor] = 0;
            }

            for (int i = 0; i < sampledColors.Length; i++)
            {
                if (foregroundMask[i])
                {
                    counts[GetNearestBlockColor(sampledColors[i])]++;
                }
            }

            List<BlockColor> palette = new List<BlockColor>();
            foreach (ColorMapping mapping in ColorMappings)
            {
                palette.Add(mapping.BlockColor);
            }

            palette.Sort((left, right) => counts[right].CompareTo(counts[left]));
            int paletteCount = Mathf.Clamp(maxActivePaletteColors, 1, ColorMappings.Length);
            if (palette.Count > paletteCount)
            {
                palette.RemoveRange(paletteCount, palette.Count - paletteCount);
            }

            if (palette.Count < 1)
            {
                palette.Add(preferredFillColor);
            }
            else if (!palette.Contains(preferredFillColor) && counts[palette[palette.Count - 1]] == 0)
            {
                palette[palette.Count - 1] = preferredFillColor;
            }

            return palette;
        }

        private static BlockColor GetNearestBlockColor(Color color, List<BlockColor> activePalette)
        {
            BlockColor nearestColor = activePalette.Count > 0 ? activePalette[0] : BlockColor.Purple;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < activePalette.Count; i++)
            {
                Color paletteColor = GetUnityColor(activePalette[i]);
                float distance = GetRgbDistanceSquared(color, paletteColor);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestColor = activePalette[i];
                }
            }

            return nearestColor;
        }

        private static void SimplifyInteriorColors(BlockColor[] cells, bool[] foregroundMask, int passes)
        {
            for (int pass = 0; pass < passes; pass++)
            {
                BlockColor[] previous = (BlockColor[])cells.Clone();
                for (int i = 0; i < ExpectedCellCount; i++)
                {
                    if (!foregroundMask[i])
                    {
                        continue;
                    }

                    BlockColor current = previous[i];
                    int sameCount = 0;
                    BlockColor majorityColor = current;
                    int majorityCount = 0;
                    foreach (ColorMapping mapping in ColorMappings)
                    {
                        int count = 0;
                        foreach (int neighbor in GetNeighborIndexes(i))
                        {
                            if (foregroundMask[neighbor] && previous[neighbor] == mapping.BlockColor)
                            {
                                count++;
                            }
                        }

                        if (mapping.BlockColor == current)
                        {
                            sameCount = count;
                        }

                        if (count > majorityCount)
                        {
                            majorityCount = count;
                            majorityColor = mapping.BlockColor;
                        }
                    }

                    if (sameCount == 0 && majorityCount >= 2)
                    {
                        cells[i] = majorityColor;
                    }
                }
            }
        }

        private static void ApplyInteriorFloodFillBias(
            BlockColor[] cells,
            bool[] foregroundMask,
            List<BlockColor> activePalette,
            BlockColor preferredFillColor,
            BlockColor preferredAccentColor,
            BlockColor preferredDetailColor)
        {
            if (!activePalette.Contains(preferredFillColor))
            {
                preferredFillColor = activePalette.Count > 0 ? activePalette[0] : preferredFillColor;
            }

            for (int i = 0; i < ExpectedCellCount; i++)
            {
                if (!foregroundMask[i] || IsBoundaryCell(foregroundMask, i))
                {
                    continue;
                }

                BlockColor current = cells[i];
                int sameCount = CountSameColorNeighbors(cells, i % OutputWidth, i / OutputWidth, current);
                bool isPreferredAccent = current == preferredAccentColor || current == preferredDetailColor;
                if (isPreferredAccent && sameCount >= 1)
                {
                    continue;
                }

                if (sameCount < 2 || current == BlockColor.None)
                {
                    cells[i] = preferredFillColor;
                }
            }
        }

        private static int ApplySilhouetteOutline(BlockColor[] cells, bool[] foregroundMask, BlockColor outlineBlockColor)
        {
            int outlineCount = 0;
            for (int i = 0; i < ExpectedCellCount; i++)
            {
                if (!foregroundMask[i] || !IsBoundaryCell(foregroundMask, i))
                {
                    continue;
                }

                cells[i] = outlineBlockColor;
                outlineCount++;
            }

            return outlineCount;
        }

        private static bool IsBoundaryCell(bool[] foregroundMask, int index)
        {
            int x = index % OutputWidth;
            int y = index / OutputWidth;
            if (x == 0 || x == OutputWidth - 1 || y == 0 || y == OutputHeight - 1)
            {
                return true;
            }

            foreach (int neighbor in GetNeighborIndexes(index))
            {
                if (!foregroundMask[neighbor])
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<int> GetNeighborIndexes(int index)
        {
            int x = index % OutputWidth;
            int y = index / OutputWidth;
            if (y > 0)
            {
                yield return ((y - 1) * OutputWidth) + x;
            }

            if (y < OutputHeight - 1)
            {
                yield return ((y + 1) * OutputWidth) + x;
            }

            if (x > 0)
            {
                yield return (y * OutputWidth) + x - 1;
            }

            if (x < OutputWidth - 1)
            {
                yield return (y * OutputWidth) + x + 1;
            }
        }

        private static Color GetUnityColor(BlockColor blockColor)
        {
            foreach (ColorMapping mapping in ColorMappings)
            {
                if (mapping.BlockColor == blockColor)
                {
                    return mapping.Color;
                }
            }

            return PurpleColor;
        }

        private static string BuildPaletteLog(List<BlockColor> activePalette)
        {
            if (activePalette.Count == 0)
            {
                return "None";
            }

            string[] names = new string[activePalette.Count];
            for (int i = 0; i < activePalette.Count; i++)
            {
                names[i] = activePalette[i].ToString();
            }

            return string.Join(",", names);
        }

        private static bool[] BuildEdgeMap(Color[] sampledColors, float edgeStrengthCutoff)
        {
            bool[] edgeMap = new bool[ExpectedCellCount];
            for (int y = 0; y < OutputHeight; y++)
            {
                for (int x = 0; x < OutputWidth; x++)
                {
                    int index = (y * OutputWidth) + x;
                    float currentBrightness = GetBrightness(sampledColors[index]);
                    float rightBrightness = x < OutputWidth - 1
                        ? GetBrightness(sampledColors[index + 1])
                        : currentBrightness;
                    float downBrightness = y > 0
                        ? GetBrightness(sampledColors[((y - 1) * OutputWidth) + x])
                        : currentBrightness;
                    float edgeStrength = Mathf.Abs(currentBrightness - rightBrightness) + Mathf.Abs(currentBrightness - downBrightness);
                    edgeMap[index] = edgeStrength >= edgeStrengthCutoff;
                }
            }

            return edgeMap;
        }

        private static int CountEdgeCells(bool[] edgeMap)
        {
            int count = 0;
            for (int i = 0; i < edgeMap.Length; i++)
            {
                if (edgeMap[i])
                {
                    count++;
                }
            }

            return count;
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
            float lightThreshold,
            bool preserveEdgeCell)
        {
            if (color.a < alphaCutoff)
            {
                return true;
            }

            if (preserveEdgeCell)
            {
                return false;
            }

            if (removeBackground && GetRgbDistanceSquared(color, backgroundColor) < backgroundToleranceSquared)
            {
                return true;
            }

            if (!removeByBrightness)
            {
                return false;
            }

            float brightness = GetBrightness(color);
            return brightness < darkThreshold || brightness > lightThreshold;
        }

        private static void ApplyNoiseCleanup(List<BlockColor> cells, int passes, int minimumSameColorNeighborCount, bool[] preservedEdgeMap)
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

                        BlockColor replacementColor = GetMostCommonNonNoneNeighborColor(previousCells, x, y);
                        if (replacementColor == BlockColor.None && IsPreservedEdgeCell(preservedEdgeMap, index))
                        {
                            continue;
                        }

                        cells[index] = replacementColor;
                    }
                }
            }
        }

        private static bool IsPreservedEdgeCell(bool[] preservedEdgeMap, int index)
        {
            return preservedEdgeMap != null && preservedEdgeMap[index];
        }

        private static void ApplyOutlineAfterCleanup(List<BlockColor> cells, bool[] edgeMap, BlockColor outlineBlockColor)
        {
            BlockColor[] previousCells = cells.ToArray();
            for (int y = 0; y < OutputHeight; y++)
            {
                for (int x = 0; x < OutputWidth; x++)
                {
                    int index = (y * OutputWidth) + x;
                    if (!edgeMap[index] || previousCells[index] != BlockColor.None)
                    {
                        continue;
                    }

                    if (HasNonNoneNeighbor(previousCells, x, y))
                    {
                        cells[index] = outlineBlockColor;
                    }
                }
            }
        }

        private static bool HasNonNoneNeighbor(BlockColor[] cells, int x, int y)
        {
            foreach (BlockColor neighborColor in GetNeighborColors(cells, x, y))
            {
                if (neighborColor != BlockColor.None)
                {
                    return true;
                }
            }

            return false;
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

        private static float GetBrightness(Color color)
        {
            return (color.r + color.g + color.b) / 3f;
        }

        private static BlockColor GetBlockColor(OutlineBlockColor outlineBlockColor)
        {
            switch (outlineBlockColor)
            {
                case OutlineBlockColor.Red:
                    return BlockColor.Red;
                case OutlineBlockColor.Blue:
                    return BlockColor.Blue;
                case OutlineBlockColor.Green:
                    return BlockColor.Green;
                case OutlineBlockColor.Yellow:
                    return BlockColor.Yellow;
                case OutlineBlockColor.Purple:
                default:
                    return BlockColor.Purple;
            }
        }

        private static BlockColor GetBlockColor(BlockColorChoice blockColorChoice)
        {
            switch (blockColorChoice)
            {
                case BlockColorChoice.Red:
                    return BlockColor.Red;
                case BlockColorChoice.Blue:
                    return BlockColor.Blue;
                case BlockColorChoice.Green:
                    return BlockColor.Green;
                case BlockColorChoice.Yellow:
                    return BlockColor.Yellow;
                case BlockColorChoice.Purple:
                default:
                    return BlockColor.Purple;
            }
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

        private enum OutlineBlockColor
        {
            Red,
            Blue,
            Green,
            Yellow,
            Purple
        }

        private enum BlockColorChoice
        {
            Red,
            Blue,
            Green,
            Yellow,
            Purple
        }

        private readonly struct GeneratedCellsResult
        {
            public GeneratedCellsResult(List<BlockColor> cells, int edgeCellCount)
                : this(cells, edgeCellCount, edgeCellCount.ToString())
            {
            }

            public GeneratedCellsResult(List<BlockColor> cells, int edgeCellCount, string activePaletteLog)
            {
                Cells = cells;
                EdgeCellCount = edgeCellCount;
                ActivePaletteLog = activePaletteLog;
            }

            public List<BlockColor> Cells { get; }
            public int EdgeCellCount { get; }
            public string ActivePaletteLog { get; }
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
