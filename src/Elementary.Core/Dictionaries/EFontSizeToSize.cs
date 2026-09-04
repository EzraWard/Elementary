using Elementary.Core.Enums;
using System.Collections.Generic;

namespace Elementary.Core.Dictionaries
{
    public static class FontSizeConverter
    {
        public static readonly Dictionary<EFontSize, int> EFontSizeToSize = new Dictionary<EFontSize, int>
        {
            { EFontSize.Small, 14 },
            { EFontSize.Medium, 18 },
            { EFontSize.Large, 22 }
        };
    }
}