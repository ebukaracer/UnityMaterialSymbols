using System;

namespace Racer.MaterialSymbols.Runtime
{
    [Serializable]
    public struct MaterialSymbolData
    {
        public char code;
        public bool fill;

        public MaterialSymbolData(char code, bool fill)
        {
            this.code = code;
            this.fill = fill;
        }
    }
}