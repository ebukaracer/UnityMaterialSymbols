using System;
using System.IO;
using TMPro;
using UnityEngine;

namespace Racer.MaterialSymbols.Editor
{
    // Only one instance is required, uncomment if missing from the /resources folder.
    // [CreateAssetMenu(fileName = "MaterialSymbolConfig")]
    public class MaterialSymbolConfig : ScriptableObject
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

        [SerializeField, Tooltip("Allow filled symbol images overwrite standard ones when both exist in the project.")]
        private bool allowFilledSymbolsOverwrite = true;

        [field: SerializeField, Tooltip("Suppress debug logs after a symbol has been created.")]
        public bool SilenceLogs { get; private set; }


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

        private void ResetToDefaultSavePath()
        {
            symbolSavePath = DefaultSymbolSavePath;
        }

        public string FileName(bool isFilled)
        {
            if (!allowFilledSymbolsOverwrite)
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
}