using Elementary.Core.Enums;
using Elementary.Core.Interfaces;

namespace Elementary.Core.Models
{
    public class AppSettings : ISettings
    {
        public ETranslation Translation { get; set; }

        public EBook Book { get; set; }

        public int Chapter { get; set; }

        public EFont Font { get; set; }

        public EFontSize FontSize { get; set; }

        public bool? ShowVerseNumbers { get; set; }

        public ETheme Theme { get; set; }
    }
}
