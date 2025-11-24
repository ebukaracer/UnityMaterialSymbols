using System;
using System.IO;
using System.Reflection;
using Racer.MaterialSymbols.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Racer.MaterialSymbols.Editor
{
    internal class SymbolToImageConverter
    {
        private static readonly Type TMPSettingsType = typeof(TMP_Settings);

        private const int FallbackSize = 64;
        private const int InitialSize = 128;
        private int _finalSize;


        public void ConvertSymbolToImage(MaterialSymbol targetMS, bool replaceWithImageComp)
        {
            GameObject canvasGo = null;
            Camera cam = null;
            RenderTexture rt = null;

            var undoGroup = 0;

            try
            {
                if (!MaterialSymbolConfig)
                {
                    Debug.LogError(
                        $"Error: \n[{nameof(MaterialSymbolConfig)}] not found in the project, reinstalling this package may fix it.");
                    return;
                }

                if (!IsTMProAvailable)
                {
                    Debug.LogError("Either TMPro package is not installed or its resources haven't been imported.");
                    return;
                }

                if (!targetMS)
                {
                    Debug.LogError(
                        $"Error: \n[{nameof(MaterialSymbol)}] is invalid, recreating a new instance may fix it.");
                    return;
                }

                // Start Undo group
                Undo.IncrementCurrentGroup();
                undoGroup = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Convert Symbol to Image");

                // Save as PNG
                var folderPath = MaterialSymbolConfig.SymbolSavePath;

                var convertCharToHex = MaterialSymbol.ConvertCharToHex(targetMS.Code);
                var symbolPath =
                    $"{folderPath}/Symbol_{convertCharToHex}{MaterialSymbolConfig.FileName(targetMS.Fill)}.png";

                var proceedWithOverwrite = 0;

                // Check if symbol already exists, and prompt if necessary
                if (File.Exists(symbolPath) && MaterialSymbolConfig.PromptBeforeOverwrite)
                {
                    proceedWithOverwrite = EditorUtility.DisplayDialogComplex("Overwrite Symbol Image?",
                        $"A symbol image for '{convertCharToHex}' already exists at the target location.\n\n" +
                        "Do you want to overwrite it?",
                        "Overwrite it and continue",
                        "Continue without overwriting", "Cancel");
                }

                if (proceedWithOverwrite == 0)
                {
                    _finalSize = InitialSize * (int)targetMS.Scale * 4;
                    _finalSize = _finalSize <= 0 ? FallbackSize : _finalSize;

                    // Create a temporary Canvas
                    canvasGo = new GameObject("TempCanvas", typeof(Canvas));
                    var canvas = canvasGo.GetComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;

                    // Create a temporary Camera for UI rendering
                    cam = new GameObject("TempUICamera").AddComponent<Camera>();
                    cam.orthographic = true;
                    cam.clearFlags = CameraClearFlags.Color;
                    cam.backgroundColor = new Color(0, 0, 0, 0); // Transparent

                    canvas.worldCamera = cam;

                    // Create a TMP UGUI text object
                    var textGo = new GameObject("TempTMPUGUI", typeof(TextMeshProUGUI));
                    textGo.transform.SetParent(canvasGo.transform, false);

                    var tmp = textGo.GetComponent<TextMeshProUGUI>();
                    tmp.text = targetMS.Symbol.code.ToString();
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.color = targetMS.color;
                    tmp.font = targetMS.Fill ? MaterialSymbolConfig.Filled : MaterialSymbolConfig.Standard;
                    tmp.fontSize = _finalSize;

                    // RectTransform must match RT pixels
                    var rect = tmp.rectTransform;
                    rect.sizeDelta = new Vector2(_finalSize, _finalSize);
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = Vector2.zero;

                    // --- Create RenderTexture ---
                    rt = new RenderTexture(_finalSize, _finalSize, 32, RenderTextureFormat.ARGB32)
                    {
                        filterMode = FilterMode.Point,
                        autoGenerateMips = false,
                        antiAliasing = 1
                    };
                    rt.Create();
                    cam.targetTexture = rt;

                    // --- Render ---
                    cam.Render();

                    // Capture RenderTexture to Texture2D
                    RenderTexture.active = rt;
                    var tex = new Texture2D(_finalSize, _finalSize, TextureFormat.RGBA32, false);
                    tex.ReadPixels(new Rect(0, 0, _finalSize, _finalSize), 0, 0);
                    tex.filterMode = FilterMode.Point;
                    tex.Apply();
                    tex = CropTransparent(tex);

                    if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                    File.WriteAllBytes(symbolPath, tex.EncodeToPNG());

                    // Refresh asset database
                    AssetDatabase.Refresh();
                }

                // Delay assignment to ensure import completion
                EditorApplication.delayCall += () =>
                {
                    if (proceedWithOverwrite == 0)
                    {
                        // Adjust texture import settings
                        var importer = AssetImporter.GetAtPath(symbolPath) as TextureImporter;
                        if (importer)
                        {
                            importer.textureType = TextureImporterType.Sprite;
                            importer.maxTextureSize = _finalSize <= 64 ? _finalSize : _finalSize / 4;
                            importer.alphaIsTransparency = true;
                            importer.spriteImportMode = SpriteImportMode.Single;
                            importer.SaveAndReimport();
                        }

                        Debug.Log($"Conversion successful. Image saved at: {symbolPath}",
                            AssetDatabase.LoadAssetAtPath<Object>(symbolPath));
                    }
                    else
                        Debug.Log("Conversion skipped.");

                    if (!replaceWithImageComp || proceedWithOverwrite == 2) return;

                    var imgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(symbolPath);
                    if (imgSprite)
                    {
                        var obj = targetMS.gameObject;
                        var raycastTarget = targetMS.raycastTarget;

                        // Register full object hierarchy for Undo
                        Undo.RegisterFullObjectHierarchyUndo(obj,
                            $"Replace {nameof(MaterialSymbol)} with Image component");

                        // Remove old component with Undo
                        Undo.DestroyObjectImmediate(targetMS);

                        // Rename with Undo
                        Undo.RecordObject(obj, "Rename object");
                        obj.name = $"Img_{imgSprite.name.Split('_')[1]}";

                        // Add Image component with Undo
                        var img = Undo.AddComponent<Image>(obj);

                        // Set properties with Undo
                        Undo.RecordObject(img, "Configure Image");
                        img.sprite = imgSprite;
                        img.preserveAspect = true;
                        img.raycastTarget = raycastTarget;
                        Debug.Log("Successfully replaced with an Image component.");
                    }
                    else
                        Debug.LogWarning(
                            "Failed to replace with an Image component using the converted sprite, you can assign it manually.");
                };
            }
            catch (Exception e)
            {
                Debug.LogError($"An unexpected error, click for more details. \n{e.Message}");
            }
            finally
            {
                // Cleanup
                RenderTexture.active = null;

                if (cam) Object.DestroyImmediate(cam.gameObject);
                if (canvasGo) Object.DestroyImmediate(canvasGo);
                if (rt)
                {
                    rt.Release();
                    Object.DestroyImmediate(rt);
                }

                GUIUtility.ExitGUI();

                // End Undo group
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        private static Texture2D CropTransparent(Texture2D source)
        {
            int minX = source.width, maxX = 0;
            int minY = source.height, maxY = 0;

            var pixels = source.GetPixels32();

            for (var y = 0; y < source.height; y++)
            {
                for (var x = 0; x < source.width; x++)
                {
                    var c = pixels[y * source.width + x];

                    if (c.a <= 0) continue; // non-transparent pixel

                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            var width = maxX - minX + 1;
            var height = maxY - minY + 1;

            var result = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };
            result.SetPixels(source.GetPixels(minX, minY, width, height));
            result.Apply();

            return result;
        }

        public static void PingConfigFile()
        {
            EditorGUIUtility.PingObject(MaterialSymbolConfig.Load);
        }

        private static bool IsTMProAvailable
        {
            get
            {
                if (!Directory.Exists("Assets/TextMesh Pro")) return false;

                if (!TMP_Settings.instance) return false;

                // Fragile reflection way(not the best, but works)
                var currentVersionField = TMPSettingsType.GetField(
                    "s_CurrentAssetVersion",
                    BindingFlags.NonPublic | BindingFlags.Static
                );

                var assetVersionField = TMPSettingsType.GetField(
                    "assetVersion",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );

                if (currentVersionField == null || assetVersionField == null) return false;

                var currentAssetVersion = (string)currentVersionField.GetValue(null);
                var assertVersion = (string)assetVersionField.GetValue(TMP_Settings.instance);

                // Compare with public instance version
                return assertVersion == currentAssetVersion;
            }
        }

        private static MaterialSymbolConfig MaterialSymbolConfig
        {
            get
            {
                var materialSymbolsConfig = MaterialSymbolConfig.Load;

                if (materialSymbolsConfig.Filled || materialSymbolsConfig.Standard)
                    return materialSymbolsConfig;

                Debug.LogError("TMP-Font reference(s) not found in the config asset!", MaterialSymbolConfig.Load);
                return null;
            }
        }
    }
}