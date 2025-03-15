using UnityEngine;

namespace com.convalise.UnityMaterialSymbols
{
    public class MaterialSymbolsFontRef : ScriptableObject
    {
        [SerializeField] private Font standard;

        [SerializeField] private Font filled;

        public Font Standard => standard;
        public Font Filled => (filled != null) ? filled : standard;

#if UNITY_EDITOR
        public string GetCodepointsEditorPath()
        {
            if (standard)
                return System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(UnityEditor.AssetDatabase.GetAssetPath(standard)) ?? string.Empty,
                    "codepoints");
            return null;
        }
#endif
    }
}