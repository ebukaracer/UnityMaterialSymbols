using Racer.MaterialSymbols.Runtime;
using UnityEditor;
using UnityEngine;

namespace Racer.MaterialSymbols.Editor
{
    [CustomPropertyDrawer(typeof(MaterialSymbolData))]
    public class MaterialSymbolDataDrawer : PropertyDrawer
    {
        public static Styles Style;
        private Styles _styles;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 3;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _styles ??= new Styles();

            if (!_styles.FontRef)
            {
                EditorGUI.LabelField(position, label, _styles.GCFontError);
                return;
            }

            var spCode = property.FindPropertyRelative("code");
            var spFill = property.FindPropertyRelative("fill");
            var charValue = System.Convert.ToChar(spCode.intValue);

            label = EditorGUI.BeginProperty(position, label, property);
            GUI.SetNextControlName(label.text);
            position = EditorGUI.PrefixLabel(position, label);
            position.width = position.height;

            if (GUI.Button(position, GUIContent.none))
            {
                GUI.FocusControl(label.text);
                MaterialSymbolSelectionWindow.Init(charValue, spFill.boolValue, (code, fill) =>
                {
                    spCode.intValue = code;
                    spFill.boolValue = fill;
                    property.serializedObject.ApplyModifiedProperties();
                });
            }

            if (property.hasMultipleDifferentValues)
            {
                GUI.Label(position, _styles.GCMixedValues, _styles.GsMixedValues);
            }
            else
            {
                _styles.GCSymbol.text = charValue.ToString();
                _styles.GCSymbol.tooltip = MaterialSymbol.ConvertCharToHex(charValue);
                _styles.GsSymbol.font = spFill.boolValue ? _styles.FontRef.Filled : _styles.FontRef.Standard;

                GUI.Label(position, _styles.GCSymbol, _styles.GsSymbol);
            }

            Style = _styles;

            EditorGUI.EndProperty();
        }


        public class Styles
        {
            public MaterialSymbolsFontRef FontRef { get; } = MaterialSymbol.LoadFontRef();

            public GUIContent GCSymbol { get; } = new();
            public GUIContent GCMixedValues { get; } = new("\u2014", "Mixed Values");

            public GUIContent GCFontError { get; } = new("Could not find fonts reference.",
                EditorGUIUtility.IconContent("console.erroricon").image);

            public GUIStyle GsSymbol { get; } = new("ControlLabel")
            {
                font = null,
                fontSize = 32,
                alignment = TextAnchor.MiddleCenter
            };

            public GUIStyle GsMixedValues { get; } = new("Label")
            {
                alignment = TextAnchor.MiddleCenter
            };
        }
    }
}