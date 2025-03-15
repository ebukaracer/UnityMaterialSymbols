using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace com.convalise.UnityMaterialSymbols
{
    public class SymbolToImageConverter
    {
        private const int FallbackSize = 64;
        private const int InitialSize = 128;
        private int _finalSize;


        public void ConvertSymbolToImage(MaterialSymbol targetMS, bool replaceWithImageComp)
        {
            try
            {
                if (!targetMS || !MaterialSymbolConfig)
                {
                    Debug.LogError(
                        $"Partial failure!\nEither '{nameof(MaterialSymbol)}' or '{nameof(MaterialSymbolConfig)}' is invalid!");
                    return;
                }

                _finalSize = InitialSize * (int)targetMS.Scale;

                // In case size is 0, return 64x64(16dp) as default
                _finalSize = Mathf.Max(_finalSize, FallbackSize);

                // Create a temporary GameObject for rendering text
                var textObj = new GameObject("TempTextRenderer");
                var tmp = textObj.AddComponent<TextMeshPro>();
                tmp.font = targetMS.Fill ? MaterialSymbolConfig.Filled : MaterialSymbolConfig.Standard;
                tmp.text = targetMS.Symbol.code.ToString();
                tmp.fontSize = 20;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = targetMS.color;

                // Create RenderTexture
                var rt = new RenderTexture(_finalSize, _finalSize, 24, RenderTextureFormat.ARGB32);
                RenderTexture.active = rt;

                // Create a temporary camera to render text
                var cam = new GameObject("TempCamera").AddComponent<Camera>();
                cam.targetTexture = rt;
                cam.clearFlags = CameraClearFlags.Color;
                cam.backgroundColor = new Color(0, 0, 0, 0); // Transparent
                cam.orthographic = true;
                cam.orthographicSize = 1;
                cam.transform.position = new Vector3(0, 0, -10);

                // Render the text
                cam.Render();

                // Capture RenderTexture to Texture2D
                var tex = new Texture2D(_finalSize, _finalSize, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, _finalSize, _finalSize), 0, 0);
                tex.Apply();

                // Save as PNG
                var folderPath = MaterialSymbolConfig.SymbolSavePath;
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
                var filePath =
                    $"{folderPath}/Symbol_{MaterialSymbol.ConvertCharToHex(targetMS.Code)}{MaterialSymbolConfig.FileName(targetMS.Fill)}.png";
                File.WriteAllBytes(filePath, tex.EncodeToPNG());

                // Cleanup
                RenderTexture.active = null;
                cam.targetTexture = null;
                Object.DestroyImmediate(textObj);
                Object.DestroyImmediate(cam.gameObject);
                rt.Release();

                // Refresh asset database
                AssetDatabase.Refresh();

                // Load the saved texture
                var importedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
                if (!importedTexture)
                {
                    Debug.LogError("Failed to load saved PNG as Texture2D.", importedTexture);
                    return;
                }

                // Adjust texture import settings
                var importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
                if (importer)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.maxTextureSize = _finalSize;
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                }

                // Delay assignment to ensure import completion
                EditorApplication.delayCall += () =>
                {
                    var imgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(filePath);

                    // Replace MaterialSymbol with Image component
                    if (replaceWithImageComp)
                    {
                        var obj = targetMS.gameObject;
                        Object.DestroyImmediate(targetMS);
                        var img = obj.AddComponent<Image>();
                        obj.name = $"Img_{imgSprite.name.Split('_')[1]}";
                        img.sprite = imgSprite;
                        img.raycastTarget = false;
                    }

                    if (!MaterialSymbolConfig.SilenceLogs)
                        Debug.Log($"Symbol converted to image and saved at: {filePath}",
                            AssetDatabase.LoadAssetAtPath<Object>(filePath));
                };
            }
            catch (Exception e)
            {
                Debug.LogError($"Total Failure!\n{e.Message}");
            }
        }

        public void PingConfigFile()
        {
            EditorGUIUtility.PingObject(LoadConfig);
        }

        private static MaterialSymbolConfig MaterialSymbolConfig
        {
            get
            {
                var materialSymbolsConfig = LoadConfig;

                if (materialSymbolsConfig.Filled || materialSymbolsConfig.Standard)
                    return materialSymbolsConfig;

                Debug.LogError("TMP font reference(s) missing in the config asset.", LoadConfig);
                return null;
            }
        }

        private static MaterialSymbolConfig LoadConfig =>
            Resources.Load<MaterialSymbolConfig>(nameof(MaterialSymbolConfig));
    }

    public static class CustomStyles
    {
        public static readonly GUIContent GenerateImageBtn = new("Convert Symbol to Image",
            "Generates a png image of the selected symbol, based upon the fill, scale and color properties.");

        public static readonly GUIContent PingConfigBtn = new("Config File?",
            "Shows the location of the config file.");

        public static readonly GUIContent ReplaceToggle = new("Replace with Image Component",
            $"Whether or not to replace this '{nameof(MaterialSymbol)}' with an 'Image' component, with its sprite set to the generated image.");
    }
}