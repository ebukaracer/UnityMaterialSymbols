using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Racer.MaterialSymbols.Runtime;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Racer.MaterialSymbols.Editor
{
    public class MaterialSymbolSelectionWindow : EditorWindow
    {
        private CodepointData[] _codepointsCollection;
        private CodepointData[] _filteredCollection;
        private MaterialSymbolsFontRef _fontRef;

        private CodepointData _selected;
        private Action<char, bool> _onSelectionChanged;

        private SearchField _searchField;
        private string _searchString = string.Empty;
        private Vector2 _scrollPos = Vector2.zero;
        private int _undoGroup;

        private bool _showNames = true;
        private bool _allowFocusSearchField = true;
        private bool _allowKeepActiveInView = true;
        private bool _enableRegexSearch;
        private int _sortId = 1;

        private bool _fill;
        private bool _focusSearchField;
        private bool _keepActiveInView;

        private int _columns;
        private int _rows;

        private Styles _styles;

        private readonly string _showNamesEpk = typeof(MaterialSymbolSelectionWindow) + ".showNames";
        private readonly string _focusSearchFieldEpk = typeof(MaterialSymbolSelectionWindow) + ".focusSearchField";
        private readonly string _keepActiveInViewEpk = typeof(MaterialSymbolSelectionWindow) + ".keepActiveInView";
        private readonly string _enableRegexSearchEpk = typeof(MaterialSymbolSelectionWindow) + ".enableRegexSearch";
        private readonly string _sortIdEpk = typeof(MaterialSymbolSelectionWindow) + ".sortId";

        public static void Init(char preSelected, bool preFilled, Action<char, bool> onSelectionChanged)
        {
            var window = GetWindow<MaterialSymbolSelectionWindow>(true);
            window.wantsMouseMove = true;
            window.LoadDependencies(preSelected, preFilled, onSelectionChanged);
            window.ShowAuxWindow();
        }

        private void LoadDependencies(char preSelected, bool preFilled, Action<char, bool> onSelectionChanged)
        {
            _searchField = new SearchField();
            _searchField.downOrUpArrowKeyPressed += () => SelectRelative(0);

            _fontRef = MaterialSymbol.LoadFontRef();

            if ((!_fontRef) || string.IsNullOrEmpty(_fontRef.GetCodepointsEditorPath()))
                return;

            _showNames = EditorPrefs.GetBool(_showNamesEpk, _showNames);
            _allowFocusSearchField = EditorPrefs.GetBool(_focusSearchFieldEpk, _allowFocusSearchField);
            _allowKeepActiveInView = EditorPrefs.GetBool(_keepActiveInViewEpk, _allowKeepActiveInView);
            _enableRegexSearch = EditorPrefs.GetBool(_enableRegexSearchEpk, _enableRegexSearch);
            _sortId = EditorPrefs.GetInt(_sortIdEpk, _sortId);

            _focusSearchField = _allowFocusSearchField;
            _keepActiveInView = _allowKeepActiveInView;

            _codepointsCollection = File.ReadAllLines(_fontRef.GetCodepointsEditorPath())
                .Select((codepoint, index) => new CodepointData(codepoint, index)).ToArray();
            RunSorter();

            _onSelectionChanged = onSelectionChanged;

            _selected = _codepointsCollection.FirstOrDefault(data => data.Code == preSelected);
            _fill = preFilled;
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Material Symbol Selection");
            minSize =
                new Vector2(
                    (Styles.IconSize + Styles.LabelHeight + Styles.Spacing) * 5f + Styles.VerticalScrollbarFixedWidth +
                    1f, (Styles.IconSize + Styles.LabelHeight + Styles.Spacing) * 6f + Styles.ToolbarFixedHeight);

            _undoGroup = Undo.GetCurrentGroup();
        }

        private void OnDisable()
        {
            _codepointsCollection = null;
            _filteredCollection = null;
            _fontRef = null;
            GC.Collect();

            Undo.SetCurrentGroupName(Regex.Replace(Undo.GetCurrentGroupName(), "\\.code|\\.fill", string.Empty));
            Undo.CollapseUndoOperations(_undoGroup);
        }

        private void OnGUI()
        {
            if (_fontRef == null)
            {
                EditorGUILayout.HelpBox("Could not find fonts reference.", MessageType.Error);
                return;
            }

            if ((_codepointsCollection == null) || (_codepointsCollection.Length == 0))
            {
                EditorGUILayout.HelpBox("Could not find codepoints data.", MessageType.Error);
                return;
            }

            _styles ??= new Styles
            {
                GsIconImage =
                {
                    font = _fill ? _fontRef.Filled : _fontRef.Standard
                }
            };

            OnHeaderGUI();
            OnBodyGUI();

            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.LeftArrow)
                {
                    SelectRelative(-1);
                    Event.current.Use();
                }

                if (Event.current.keyCode == KeyCode.RightArrow)
                {
                    SelectRelative(+1);
                    Event.current.Use();
                }

                if (Event.current.keyCode == KeyCode.UpArrow)
                {
                    SelectRelative(-_columns);
                    Event.current.Use();
                }

                if (Event.current.keyCode == KeyCode.DownArrow)
                {
                    SelectRelative(+_columns);
                    Event.current.Use();
                }

                if (Event.current.keyCode == KeyCode.PageUp)
                {
                    SelectRelative(-(_columns * 6));
                    Event.current.Use();
                }

                if (Event.current.keyCode == KeyCode.PageDown)
                {
                    SelectRelative(+(_columns * 6));
                    Event.current.Use();
                }

                if (Event.current.keyCode == KeyCode.Home)
                {
                    SelectAbsolute(0);
                    Event.current.Use();
                }

                if (Event.current.keyCode == KeyCode.End)
                {
                    SelectAbsolute(_filteredCollection.Length - 1);
                    Event.current.Use();
                }

                if ((Event.current.keyCode == KeyCode.Return) || (Event.current.keyCode == KeyCode.KeypadEnter))
                {
                    if (!_searchField.HasFocus())
                    {
                        EditorApplication.delayCall += Close;
                        GUIUtility.ExitGUI();
                        Event.current.Use();
                    }
                }

                if (Event.current.keyCode == KeyCode.Escape)
                {
                    Undo.RevertAllDownToGroup(_undoGroup);
                    // Event.current.Use();
                }
            }
        }

        private void OnHeaderGUI()
        {
            EditorGUILayout.BeginHorizontal(_styles.GsToolbar);
            var rectSearchField = GUILayoutUtility.GetRect(GUIContent.none, _styles.GsToolbarSearchField,
                GUILayout.ExpandWidth(true));
            var rectFillButton =
                GUILayoutUtility.GetRect(_styles.GCLabelFill, _styles.GsToolbarButton, GUILayout.Width(64f));
            var rectSettingsButton = GUILayoutUtility.GetRect(Styles.ToolbarFixedHeight, Styles.ToolbarFixedHeight,
                GUILayout.ExpandWidth(false));
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            _searchString = _searchField.OnToolbarGUI(rectSearchField, _searchString);
            if (EditorGUI.EndChangeCheck())
            {
                RunFilter();
            }

            if (_focusSearchField)
            {
                _searchField.SetFocus();
                _focusSearchField = false;
            }

            EditorGUI.BeginChangeCheck();
            _fill = EditorGUI.Toggle(rectFillButton, _fill, _styles.GsToolbarButton);
            if (EditorGUI.EndChangeCheck())
            {
                _styles.GsIconImage.font = _fill ? _fontRef.Filled : _fontRef.Standard;
                if (_selected != null)
                    Select(_selected, false);
            }

            EditorGUI.LabelField(rectFillButton, _styles.GCLabelFill, _styles.GsToolbarLabel);

            if (GUI.Button(rectSettingsButton, GUIContent.none, _styles.GsToolbarButton))
            {
                GUIUtility.keyboardControl = 0;
                var menu = new GenericMenu();
                menu.AddItem(_styles.GCMenuSort0, _sortId == 0, ChangeSort, 0);
                menu.AddItem(_styles.GCMenuSort1, _sortId == 1, ChangeSort, 1);
                menu.AddItem(_styles.GCMenuSort2, _sortId == 2, ChangeSort, 2);
                menu.AddSeparator(string.Empty);
                menu.AddItem(_styles.GCMenuShowNames, _showNames, ToggleShowNames);
                menu.AddItem(_styles.GCMenuKeepView, _allowKeepActiveInView, ToggleKeepView);
                menu.AddItem(_styles.GCMenuFocusSearch, _allowFocusSearchField, ToggleFocusSearch);
                menu.AddItem(_styles.GCMenuEnableRegex, _enableRegexSearch, ToggleRegexSearch);
                menu.AddSeparator(string.Empty);
                menu.AddItem(_styles.GCMenuRepository, false, OpenRepositorySite);
                menu.AddItem(_styles.GCMenuGoogleFonts, false, OpenGoogleFontsWebsite);
                menu.DropDown(rectSettingsButton);
            }

            rectSettingsButton.xMin += Styles.OptionsMenuLeftOffset;
            rectSettingsButton.yMin += Styles.OptionsMenuTopOffset;
            EditorGUI.LabelField(rectSettingsButton, _styles.GCLabelOptions, _styles.GsToolbarOptions);
        }

        private void OnBodyGUI()
        {
            var iconRect = new Rect(0f, 0f, Styles.IconSize + Styles.LabelHeight, Styles.IconSize);
            var labelRect = new Rect(0f, 0f, iconRect.width, Styles.LabelHeight);
            var buttonRect = new Rect(0f, 0f, iconRect.width + Styles.Spacing,
                iconRect.height + labelRect.height + Styles.Spacing);

            if (!_showNames)
            {
                iconRect.width -= Styles.LabelHeight;
                buttonRect.width -= Styles.LabelHeight;
                buttonRect.height -= Styles.LabelHeight;
                labelRect.height = 0f;
            }

            _columns = Mathf.FloorToInt((position.width - Styles.VerticalScrollbarFixedWidth) /
                                        (iconRect.width + Styles.Spacing));
            _rows = Mathf.CeilToInt(_filteredCollection.Length / (float)_columns);

            var groupRect = new Rect(0f, Styles.ToolbarFixedHeight, position.width,
                position.height - Styles.ToolbarFixedHeight);

            GUI.BeginGroup(groupRect);

            var scrollRect = new Rect(0f, 0f, groupRect.width, groupRect.height);
            var viewRect = new Rect(0f, 0f, scrollRect.width - Styles.VerticalScrollbarFixedWidth,
                _rows * (iconRect.height + labelRect.height + Styles.Spacing));

            _scrollPos = GUI.BeginScrollView(scrollRect, _scrollPos, viewRect);

            var focus = !_searchField.HasFocus();

            for (var i = 0; i < _filteredCollection.Length; i++)
            {
                var data = _filteredCollection[i];

                iconRect.x = ((i % _columns) * (iconRect.width + Styles.Spacing)) + (Styles.Spacing * 0.5f);
                iconRect.y = ((i / _columns) * (iconRect.height + labelRect.height + Styles.Spacing)) +
                             (Styles.Spacing * 0.5f);

                labelRect.x = iconRect.x;
                labelRect.y = iconRect.y + iconRect.height;

                buttonRect.x = iconRect.x - (Styles.Spacing * 0.5f);
                buttonRect.y = iconRect.y - (Styles.Spacing * 0.5f);

                var active = (data == _selected);
                var visible = (buttonRect.yMax > _scrollPos.y) && (buttonRect.yMin < _scrollPos.y + scrollRect.height);

                if (active && _keepActiveInView)
                {
                    if (buttonRect.yMax > _scrollPos.y + scrollRect.height)
                        _scrollPos.y = buttonRect.yMax - scrollRect.height;
                    else if (buttonRect.yMin < _scrollPos.y)
                        _scrollPos.y = buttonRect.yMin;

                    _keepActiveInView = false;
                    Repaint();
                }

                if (!visible)
                    continue;

                var hover = buttonRect.Contains(Event.current.mousePosition);

                if (Event.current.type == EventType.Repaint)
                {
#if UNITY_2022_1_OR_NEWER
                    _styles.GsIconSelection.Draw(buttonRect, false, active, hover || active, active && focus);
#else
                    _styles.GsIconSelection.Draw(buttonRect, false, active, active, active && focus);
#endif

                    _styles.GsIconImage.Draw(iconRect, data.GCCode, false, false, active, active && focus);
                    if (_showNames)
                        _styles.GsIconLabel.Draw(labelRect, data.GCLabel, false, false, active, active && focus);
                }

                GUI.Label(_showNames ? labelRect : iconRect, data.GCTooltip, GUIStyle.none);

                if (hover && (Event.current.type == EventType.MouseDown))
                {
                    Select(data);
                    if (Event.current.clickCount == 2)
                    {
                        EditorApplication.delayCall += Close;
                        GUIUtility.ExitGUI();
                    }

                    Event.current.Use();
                }
            }

            GUI.EndScrollView();
            GUI.EndGroup();
        }

        private void ToggleShowNames()
        {
            EditorPrefs.SetBool(_showNamesEpk, _showNames = !_showNames);
            _keepActiveInView = _allowKeepActiveInView;
        }

        private void ToggleFocusSearch()
        {
            EditorPrefs.SetBool(_focusSearchFieldEpk, _allowFocusSearchField = !_allowFocusSearchField);
        }

        private void ToggleRegexSearch()
        {
            EditorPrefs.SetBool(_enableRegexSearchEpk, _enableRegexSearch = !_enableRegexSearch);
            RunFilter();
        }

        private void ToggleKeepView()
        {
            EditorPrefs.SetBool(_keepActiveInViewEpk, _allowKeepActiveInView = !_allowKeepActiveInView);
        }

        private static void OpenRepositorySite()
        {
            Application.OpenURL("https://github.com/ebukaracer/UnityMaterialSymbols");
        }

        private void OpenGoogleFontsWebsite()
        {
            Application.OpenURL("https://fonts.google.com/icons");
        }

        private void ChangeSort(object i)
        {
            EditorPrefs.SetInt(_sortIdEpk, _sortId = (int)i);
            RunSorter();
        }

        private void RunSorter()
        {
            Array.Sort(_codepointsCollection, (data, other) =>
            {
                switch (_sortId)
                {
                    case 2: return string.Compare(data.Hex, other.Hex, StringComparison.Ordinal);
                    case 1: return string.Compare(data.Name, other.Name, StringComparison.Ordinal);
                    default: return data.Index.CompareTo(other.Index);
                }
            });

            RunFilter();
        }

        private void RunFilter()
        {
            _filteredCollection = string.IsNullOrEmpty(_searchString)
                ? _codepointsCollection
                : _codepointsCollection.Where(DoesItemMatchFilter).ToArray();

            _keepActiveInView = _allowKeepActiveInView;
            _scrollPos.y = 0f;
            Repaint();
        }

        private bool DoesItemMatchFilter(CodepointData data)
        {
            if (_enableRegexSearch)
            {
                try
                {
                    return Regex.IsMatch(data.Label, _searchString, RegexOptions.IgnoreCase);
                }
                catch
                {
                    return false;
                }
            }

            return data.Label.IndexOf(_searchString, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void SelectRelative(int delta)
        {
            if (_searchField.HasFocus())
            {
                GUIUtility.keyboardControl = 0;
                delta = 0;
            }

            SelectAbsolute(Array.IndexOf(_filteredCollection, _selected) + delta);
        }

        private void SelectAbsolute(int index)
        {
            index = Mathf.Clamp(index, 0, _filteredCollection.Length - 1);
            Select(_filteredCollection[index]);
        }

        private void Select(CodepointData data, bool keep = true)
        {
            GUIUtility.keyboardControl = 0;
            _selected = data;
            _onSelectionChanged.Invoke(data.Code, _fill);
            _keepActiveInView = keep;
            Repaint();
        }

        [Serializable]
        public class CodepointData
        {
            public string Name { get; private set; }
            public string Hex { get; private set; }
            public int Index { get; private set; }

            public string Label { get; private set; }
            public char Code { get; private set; }

            public GUIContent GCLabel { get; private set; }
            public GUIContent GCCode { get; private set; }
            public GUIContent GCTooltip { get; private set; }

            public CodepointData(string codepoint, int index)
            {
                var data = codepoint.Split(' ');
                Init(data[0], data[1], index);
            }

            public CodepointData(string name, string hex, int index)
            {
                Init(name, hex, index);
            }

            private void Init(string name, string hex, int index)
            {
                Name = name;
                Hex = hex;
                Index = index;

                Label = $"{Name.ToLowerInvariant().Replace('_', ' ')} ({Hex})";
                Code = MaterialSymbol.ConvertHexToChar(Hex);

                GCLabel = new GUIContent(Label);
                GCCode = new GUIContent(Code.ToString());
                GCTooltip = new GUIContent(string.Empty, Label);
            }
        }

        private class Styles
        {
            public const int IconSize = 56;
            public const int LabelHeight = 26;
            public const int Spacing = 9;

#if UNITY_2019_3_OR_NEWER
            public const int OptionsMenuLeftOffset = 2;
            public const int OptionsMenuTopOffset = 2;
#else
            public const int OptionsMenuLeftOffset = 1;
            public const int OptionsMenuTopOffset = 5;
#endif

            public static float VerticalScrollbarFixedWidth => GUI.skin.verticalScrollbar.fixedWidth;

            public static float ToolbarFixedHeight => EditorStyles.toolbar.fixedHeight;

            public GUIContent GCLabelFill { get; }
            public GUIContent GCLabelOptions { get; }

            public GUIContent GCMenuSort0 { get; }
            public GUIContent GCMenuSort1 { get; }
            public GUIContent GCMenuSort2 { get; }
            public GUIContent GCMenuShowNames { get; }
            public GUIContent GCMenuFocusSearch { get; }
            public GUIContent GCMenuEnableRegex { get; }
            public GUIContent GCMenuKeepView { get; }

            public GUIContent GCMenuRepository { get; private set; }
            public GUIContent GCMenuGoogleFonts { get; private set; }
            public GUIStyle GsToolbar { get; }
            public GUIStyle GsToolbarButton { get; }
            public GUIStyle GsToolbarSearchField { get; }
            public GUIStyle GsToolbarLabel { get; }
            public GUIStyle GsToolbarOptions { get; }

            public GUIStyle GsIconSelection { get; }
            public GUIStyle GsIconImage { get; }
            public GUIStyle GsIconLabel { get; }

            public Styles()
            {
                GCLabelFill = new GUIContent("Fill", "Switch between filled and standard styles.");
                GCLabelOptions = new GUIContent(string.Empty, "Options");

                GsToolbar = new GUIStyle("Toolbar");
                GsToolbarButton = new GUIStyle("ToolbarButton");

                GCMenuSort0 = new GUIContent("Sort by Font Index");
                GCMenuSort1 = new GUIContent("Sort by Name");
                GCMenuSort2 = new GUIContent("Sort by Code");
                GCMenuShowNames = new GUIContent("Show Labels");
                GCMenuFocusSearch = new GUIContent("Focus Search Field on Open");
                GCMenuEnableRegex = new GUIContent("Search Using Regular Expression");
                GCMenuKeepView = new GUIContent("Keep Selection in View");
                GCMenuRepository = new GUIContent("Open GitHub Repository...");
                GCMenuGoogleFonts = new GUIContent("Open Material Symbols Library...");

                GsToolbarSearchField = new GUIStyle("TextField");

                GsToolbarLabel = new GUIStyle("ControlLabel")
                {
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(0, 0, 0, 0)
                };

                GsToolbarOptions = new GUIStyle("PaneOptions");

                GsIconSelection = new GUIStyle("PR Label")
                {
                    fixedHeight = 0,
                    padding = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(0, 0, 0, 0)
                };

                GsIconLabel = new GUIStyle("ControlLabel")
                {
                    fontSize = 10,
                    alignment = TextAnchor.UpperCenter,
                    wordWrap = true,
                    padding = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(0, 0, 0, 0)
                };

                GsIconImage = new GUIStyle("ControlLabel")
                {
                    font = null,
                    fontSize = 38,
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(0, 0, 0, 0)
                };

#if !UNITY_2019_3_OR_NEWER
                GsIconLabel.onFocused.textColor = Color.white;
                GsIconImage.onFocused.textColor = Color.white;
#endif
            }
        }
    }
}