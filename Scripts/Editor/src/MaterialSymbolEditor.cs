using System.Reflection;
using Racer.MaterialSymbols.Runtime;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace Racer.MaterialSymbols.Editor
{
    [CustomEditor(typeof(MaterialSymbol), true), CanEditMultipleObjects]
    public class MaterialSymbolEditor : UnityEditor.UI.TextEditor
    {
        private SerializedProperty _spSymbol;
        private SerializedProperty _spFill;
        private SerializedProperty _spScale;
        private SerializedProperty _spAlignment;

        private FontDataDrawer _pdFontData;
        private SymbolToImageConverter _symbolToImageConverter;

        private GUIStyle _gsPreviewSymbol;
        private SerializedProperty _replaceWithImageComp;

        protected override void OnEnable()
        {
            base.OnEnable();

            _symbolToImageConverter = new SymbolToImageConverter();

            _spSymbol = serializedObject.FindProperty("symbol");
            _spFill = serializedObject.FindProperty("symbol.fill");
            _spScale = serializedObject.FindProperty("scale");
            _spAlignment = serializedObject.FindProperty("m_FontData.m_Alignment");
            _replaceWithImageComp = serializedObject.FindProperty("replaceWithImageComp");

            _pdFontData = new FontDataDrawer();

            _gsPreviewSymbol = new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,
                normal =
                {
                    textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black
                }
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_spSymbol);
            EditorGUILayout.PropertyField(_spFill);
            EditorGUILayout.PropertyField(_spScale);

            DoTextAlignmentControl(_pdFontData, _spAlignment);

            EditorGUILayout.Space();

            AppearanceControlsGUI();
            RaycastControlsGUI();
#if UNITY_2019_4_OR_NEWER
            MaskableControlsGUI();
#endif

            #region Convert Symbol to Image

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            _replaceWithImageComp.boolValue =
                EditorGUILayout.ToggleLeft(CustomStyles.ReplaceToggle, _replaceWithImageComp.boolValue);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(5);

            if (GUILayout.Button(CustomStyles.GenerateImageBtn))
            {
                foreach (var obj in targets)
                    _symbolToImageConverter.ConvertSymbolToImage((MaterialSymbol)obj, _replaceWithImageComp.boolValue);
            }

            if (GUILayout.Button(CustomStyles.PingConfigBtn))
                SymbolToImageConverter.PingConfigFile();

            EditorGUILayout.EndVertical();

            #endregion
        }

        public override bool HasPreviewGUI()
        {
            return true;
        }

        public override string GetInfoString()
        {
            var targetMS = target as MaterialSymbol;
            return $"Code: {MaterialSymbol.ConvertCharToHex(targetMS.Symbol.code)}";
        }

        public override void OnPreviewGUI(Rect drawArea, GUIStyle _)
        {
            var targetMS = target as MaterialSymbol;
            var size = (int)Mathf.Min(drawArea.width, drawArea.height);

            drawArea.x += (drawArea.width * 0.5f) - (size * 0.5f);
            drawArea.y += (drawArea.height * 0.5f) - (size * 0.5f);
            drawArea.width = drawArea.height = size;

            _gsPreviewSymbol.fontSize = size;
            _gsPreviewSymbol.font = targetMS.font;

            EditorGUI.DrawTextureTransparent(drawArea, null, ScaleMode.StretchToFill, 1f);
            EditorGUI.LabelField(drawArea, targetMS.Symbol.code.ToString(), _gsPreviewSymbol);
        }

        /// <summary> Reflection for the private synonymous method from the FontDataDrawer class. </summary>
        private static void DoTextAlignmentControl(FontDataDrawer propertyDrawer, SerializedProperty property)
        {
            var position = GUILayoutUtility.GetRect(0f, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(false));
            try
            {
                if (_miDoTextAlignmentControl == null)
                    _miDoTextAlignmentControl = typeof(FontDataDrawer).GetMethod("DoTextAligmentControl",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                _miDoTextAlignmentControl.Invoke(propertyDrawer, new object[] { position, property });
            }
            catch (System.Exception)
            {
                EditorGUI.PropertyField(position, property);
            }
        }

        private static MethodInfo _miDoTextAlignmentControl;
    }
}