using System.IO;
using UnityEngine;

namespace Racer.MaterialSymbols.Runtime
{
    public class MaterialSymbolsFontRef : ScriptableObject
    {
        [SerializeField] private Font standard;

        [SerializeField] private Font filled;

        public Font Standard => standard;
        public Font Filled => (filled) ? filled : standard;

#if UNITY_EDITOR
        public string GetCodepointsEditorPath()
        {
            if (standard)
                return Path.Combine(
                    Path.GetDirectoryName(UnityEditor.AssetDatabase.GetAssetPath(standard)) ?? string.Empty,
                    "codepoints");
            return null;
        }
#endif
    }
}