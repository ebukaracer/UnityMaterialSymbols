using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Racer.MaterialSymbols.Editor
{
    // Only one instance is required.
    // DEV-MODE: uncomment below if missing from the /resources folder.
    // [CreateAssetMenu(fileName = "MaterialSymbolConfig")]
    internal class MaterialSymbolConfig : ScriptableObject
    {
        private static MaterialSymbolConfig _instance;
        private const string DefaultSymbolSavePath = "Assets/Material Symbols";

        [field: SerializeField] public TMP_FontAsset Standard { get; private set; }
        [field: SerializeField] public TMP_FontAsset Filled { get; private set; }

        [Space(10)]
        [SerializeField,
         Tooltip(
             "The directory path where created symbol images will be saved.\nRight-click to reset this path to the default location."),
         ContextMenuItem(nameof(ResetToDefaultSavePath), nameof(ResetToDefaultSavePath))]
        private string symbolSavePath = DefaultSymbolSavePath;

        [SerializeField,
         Tooltip(
             "When checked, filled symbols will automatically overwrite standard symbols and no name distinction will be used.")]
        private bool useDistinctNames;

        [SerializeField,
         Tooltip("When checked, prompts before overwriting existing symbol images.")]
        private bool promptBeforeOverwrite = true;

        [SerializeField,
         Tooltip("Create filled symbols instead of non-filled ones.")]
        private bool preferFilledSymbol;

        [SerializeField,
         Tooltip("Create perfectly square symbols instead of rectangular ones.")]
        private bool preferSquaredButton;

        public bool PreferFilledSymbol => preferFilledSymbol;

        public bool PreferSquaredButton => preferSquaredButton;

        public bool PromptBeforeOverwrite => promptBeforeOverwrite;

        private void ResetToDefaultSavePath()
        {
            symbolSavePath = DefaultSymbolSavePath;
        }

        public string SymbolSavePath
        {
            get
            {
                try
                {
                    return symbolSavePath;
                }
                catch (Exception)
                {
                    Debug.LogWarning(
                        $"Path supplied was invalid, so a new path was used.\nInvalid path: {symbolSavePath}, Fallback path: {DefaultSymbolSavePath}");
                    return DefaultSymbolSavePath;
                }
            }
        }

        public string FileName(bool isFilled)
        {
            if (useDistinctNames)
                return isFilled ? "_fill" : "_no-fill";

            return string.Empty;
        }

        public static MaterialSymbolConfig Load
        {
            get
            {
                if (_instance) return _instance;

                _instance = Resources.Load<MaterialSymbolConfig>(nameof(MaterialSymbolConfig));

                if (!_instance)
                    throw new FileNotFoundException(
                        $"{nameof(MaterialSymbolConfig)} asset not found! Re-install this package to fix the issue.");

                return _instance;
            }
        }
    }

    [CustomEditor(typeof(MaterialSymbolConfig))]
    internal class MaterialSymbolConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "Assuming the fields are greyed out(non-editable), then enable Debug mode while this asset is focused in the inspector, " +
                "then switch back to Normal mode. The non-editable fields will become editable afterwards.\n\n" +
                "Tip: Right-click the 'Symbol Save Path' field to reset to the default location.", MessageType.Info);
        }
    }
}