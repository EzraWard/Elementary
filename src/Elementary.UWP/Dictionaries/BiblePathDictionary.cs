using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elementary.UWP.Dictionaries
{
    public class BiblePathDictionary
    {
        public static readonly Dictionary<string, string> BibleDictionary = new Dictionary<string, string>
        {
            { "NET", "ms-appx:///Content/NET" },
            { "KJV", "ms-appx:///Content/KJV"  },
            { "ASV", "ms-appx:///Content/ASV"  }
        };
    }
}
