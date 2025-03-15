using System;
using TMPro;
using UnityEngine;

namespace com.convalise.UnityMaterialSymbols
{
    // Only one instance is required, uncomment if missing from the /resources folder.
    // [CreateAssetMenu(fileName = "MaterialSymbolsConfig")]
    public class MaterialSymbolConfig : ScriptableObject
    {
        private const string DefaultSymbolSavePath = "Assets/Material Symbols";

        [field: SerializeField] public TMP_FontAsset Standard { get; private set; }
        [field: SerializeField] public TMP_FontAsset Filled { get; private set; }

        [Space(10)]
        [SerializeField, Tooltip("Path to save the created symbol images.\nRight click to reset it to default."),
         ContextMenuItem(nameof(ResetToDefaultSavePath), nameof(ResetToDefaultSavePath))]
        private string symbolSavePath = DefaultSymbolSavePath;

        [SerializeField, Tooltip("Allow filled symbols to overwrite standard symbols when both exist.")]
        private bool allowFilledSymbolsOverride = true;

        [field: SerializeField, Tooltip("Prevent debug logs after symbol has been created.")]
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
            if (!allowFilledSymbolsOverride)
                return isFilled ? "_fill" : "_no-fill";

            return string.Empty;
        }
    }
}