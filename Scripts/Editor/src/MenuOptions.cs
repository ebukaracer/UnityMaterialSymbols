using System.IO;
using Racer.MaterialSymbols.Runtime;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Racer.MaterialSymbols.Editor
{
    internal static class MenuOptions
    {
        private static RemoveRequest _removeRequest;
        private const string PkgId = "com.racer.material-symbols";
        private const string MenuPathRoot = "Racer/Google/";
        private const string SamplesPath = "Assets/Samples/Material Symbols";

        [MenuItem(MenuPathRoot + "New Material Symbol", priority = 0)]
        private static void NewMaterialSymbolContext(MenuCommand menuCommand)
        {
            CreateMaterialSymbol(menuCommand);
        }

        [MenuItem(MenuPathRoot + "Button - Material Symbol", priority = 1)]
        private static void MaterialSymbolButtonContext(MenuCommand menuCommand)
        {
            CreateMaterialSymbolButton(menuCommand);
        }

        [MenuItem("GameObject/UI/Google/New Material Symbol", false, 9)]
        public static void CreateMaterialSymbol(MenuCommand menuCommand)
        {
            var parent = GetParent(menuCommand);

            // Create the MaterialSymbol object
            var gameObject = new GameObject("MaterialSymbol", typeof(MaterialSymbol))
            {
                layer = LayerMask.NameToLayer("UI")
            };
            GameObjectUtility.SetParentAndAlign(gameObject, parent);
            Undo.RegisterCreatedObjectUndo(gameObject, "Create " + gameObject.name);
            Selection.activeObject = gameObject;
        }

        [MenuItem("GameObject/UI/Google/Button - Material Symbol", false, 10)]
        public static void CreateMaterialSymbolButton(MenuCommand menuCommand)
        {
            var parent = GetParent(menuCommand);

            // Create the Button object
            var button = new GameObject("Button", typeof(Button), typeof(Image))
            {
                layer = LayerMask.NameToLayer("UI")
            };

            // Change the rect transform of the MaterialSymbol to fit inside the button
            var buttonRect = button.GetComponent<RectTransform>();
            if (MaterialSymbolConfig && !MaterialSymbolConfig.PreferSquaredButton)
                buttonRect.sizeDelta = new Vector2(150, 50);
            else
                buttonRect.sizeDelta = new Vector2(50, 50);

            // Add the default UI sprite to the button's Image component
            var image = button.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;

            // Set the button as a child of the selected parent or canvas
            GameObjectUtility.SetParentAndAlign(button, parent);

            // Create the MaterialSymbol object
            var materialSymbol = new GameObject("MaterialSymbol", typeof(MaterialSymbol))
            {
                layer = LayerMask.NameToLayer("UI")
            };

            // Adjust the MaterialSymbol's transform to fit inside the button
            var symbolRect = materialSymbol.GetComponent<RectTransform>();
            symbolRect.anchorMin = Vector2.zero;
            symbolRect.anchorMax = Vector2.one;
            symbolRect.offsetMin = Vector2.zero;
            symbolRect.offsetMax = Vector2.zero;

            // Set the default color and scale for the MaterialSymbol component
            var materialSymbolComponent = materialSymbol.GetComponent<MaterialSymbol>();
            materialSymbolComponent.Scale = 0.5f;
            materialSymbolComponent.color = new Color(76, 76, 76, 255) / 255f;
            materialSymbolComponent.Fill = MaterialSymbolConfig && MaterialSymbolConfig.PreferFilledSymbol;

            GameObjectUtility.SetParentAndAlign(materialSymbol, button);

            Undo.RegisterCreatedObjectUndo(button, $"Create {button.name}");
        }

        private static GameObject GetParent(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;

            // Check if the selected parent or its ancestors contain a canvas
            var existingCanvas = parent ? parent.GetComponentInParent<Canvas>() : null;

            // If no canvas is found, try to find any canvas in the scene
            if (!existingCanvas)
                existingCanvas = Object.FindFirstObjectByType<Canvas>();

            // If there is still no canvas, create one
            if (!existingCanvas)
            {
                var canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))
                {
                    layer = LayerMask.NameToLayer("UI")
                };
                canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                Undo.RegisterCreatedObjectUndo(canvas, "Create " + canvas.name);

#if UNITY_2022_3_OR_NEWER
                if (!Object.FindAnyObjectByType<EventSystem>())
#else
                if (!Object.FindFirstObjectByType<EventSystem>())
#endif
                {
                    var eventSystem = new GameObject("EventSystem", typeof(EventSystem),
                        typeof(StandaloneInputModule));
                    Undo.RegisterCreatedObjectUndo(eventSystem, "Create " + eventSystem.name);
                }

                parent = canvas;
            }
            else
            {
                // Use the existing canvas or MaterialSymbol as the parent
                parent ??= existingCanvas.gameObject;
            }

            return parent;
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


        [MenuItem(MenuPathRoot + "Remove Package(recommended)", priority = 1)]
        private static void RemovePackage()
        {
            _removeRequest = Client.Remove(PkgId);
            EditorApplication.update += RemoveRequest;
        }

        private static void RemoveRequest()
        {
            if (!_removeRequest.IsCompleted) return;

            switch (_removeRequest.Status)
            {
                case StatusCode.Success:
                    if (Directory.Exists(SamplesPath))
                    {
                        Directory.Delete(SamplesPath, true);
                        File.Delete(SamplesPath + ".meta");
                    }

                    break;
                case >= StatusCode.Failure:
                    Debug.LogError($"Failed to remove package: '{PkgId}'\n{_removeRequest.Error.message}");
                    break;
            }

            EditorApplication.update -= RemoveRequest;
        }
    }
}