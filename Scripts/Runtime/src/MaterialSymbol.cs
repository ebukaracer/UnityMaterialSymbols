using UnityEngine;
using UnityEngine.UI;

namespace Racer.MaterialSymbols.Runtime
{
    public class MaterialSymbol : Text
    {
        [SerializeField] private MaterialSymbolData symbol;

        [SerializeField, Range(0f, 2f)] private float scale = 1f;

        [SerializeField] public bool replaceWithImageComp;

        public MaterialSymbolData Symbol
        {
            get => symbol;
            set
            {
                symbol = value;
                UpdateSymbol();
            }
        }

        public char Code
        {
            get => symbol.code;
            set
            {
                symbol.code = value;
                UpdateSymbol();
            }
        }

        public bool Fill
        {
            get => symbol.fill;
            set
            {
                symbol.fill = value;
                UpdateSymbol();
            }
        }

        public float Scale
        {
            get => scale;
            set
            {
                scale = value;
                UpdateFontSize();
            }
        }

        private MaterialSymbolsFontRef _fontRef;


        protected override void Start()
        {
            base.Start();

            if (string.IsNullOrEmpty(base.text))
            {
                Init();
            }

            if (font == null)
            {
                UpdateSymbol();
            }
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            Init();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            UpdateSymbol();
            UpdateFontSize();
        }
#endif

        /// <summary> Properly initializes base Text class. </summary>
        private void Init()
        {
            symbol = new MaterialSymbolData('\uef55', false);

            base.text = null;
            font = null;
            base.color = Color.white;
            base.material = null;
            alignment = TextAnchor.MiddleCenter;
            supportRichText = false;
            horizontalOverflow = HorizontalWrapMode.Overflow;
            verticalOverflow = VerticalWrapMode.Overflow;

            UpdateSymbol();
            UpdateFontSize();
        }

        /// <summary> Updates font based on fill state. </summary>
        private void UpdateSymbol()
        {
            if (_fontRef == null)
                _fontRef = LoadFontRef();

            if (_fontRef != null)
                font = symbol.fill ? _fontRef.Filled : _fontRef.Standard;

            base.text = symbol.code.ToString();
        }

        /// <summary> Updates font size based on transform size. </summary>
        private void UpdateFontSize()
        {
            fontSize = Mathf.FloorToInt(Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) *
                                        scale);
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            UpdateFontSize();
        }

        /// <summary> Loads the font ref asset from Resources. </summary>
        public static MaterialSymbolsFontRef LoadFontRef()
        {
            return Resources.Load<MaterialSymbolsFontRef>("MaterialSymbolsFontRef");
        }

        /// <summary> Converts from unicode char to hexadecimal string representation. </summary>
        public static string ConvertCharToHex(char code)
        {
            try
            {
                return System.Convert.ToString(code, 16);
            }
            catch (System.Exception)
            {
                return default(string);
            }
        }

        /// <summary> Converts from hexadecimal string representation to unicode char. </summary>
        public static char ConvertHexToChar(string hex)
        {
            try
            {
                return System.Convert.ToChar(System.Convert.ToInt32(hex, 16));
            }
            catch (System.Exception)
            {
                return default;
            }
        }
    }
}