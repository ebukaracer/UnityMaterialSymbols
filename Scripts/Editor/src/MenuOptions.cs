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
        private static void InitMaterialSymbol(MenuCommand menuCommand)
        {
            CreateMaterialSymbol(menuCommand);
        }

        [MenuItem("GameObject/UI/Google/New Material Symbol", false, 10)]
        public static void CreateMaterialSymbol(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;

            // Check if the selected parent or its ancestors contain a canvas
            var existingCanvas = parent != null ? parent.GetComponentInParent<Canvas>() : null;

            // If no canvas is found, try to find any canvas in the scene
            if (existingCanvas == null)
                existingCanvas = Object.FindFirstObjectByType<Canvas>();

            // If there is still no canvas, create one
            if (existingCanvas == null)
            {
                var canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))
                {
                    layer = LayerMask.NameToLayer("UI")
                };
                canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                Undo.RegisterCreatedObjectUndo(canvas, "Create " + canvas.name);

#if UNITY_2022_3_OR_NEWER
                if (Object.FindAnyObjectByType<EventSystem>() == null)
#else
                if (Object.FindObjectOfType<EventSystem>() == null)
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

            // Create the MaterialSymbol object
            var gameObject = new GameObject("MaterialSymbol", typeof(MaterialSymbol))
            {
                layer = LayerMask.NameToLayer("UI")
            };
            GameObjectUtility.SetParentAndAlign(gameObject, parent);
            Undo.RegisterCreatedObjectUndo(gameObject, "Create " + gameObject.name);
            Selection.activeObject = gameObject;
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
                    Debug.LogWarning($"Failed to remove package: '{PkgId}'\n{_removeRequest.Error.message}");
                    break;
            }

            EditorApplication.update -= RemoveRequest;
        }
    }
}