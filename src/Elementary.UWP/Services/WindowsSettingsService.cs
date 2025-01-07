using Elementary.Core.Interfaces;
using Elementary.Core.Models;
using Windows.Storage;

namespace Elementary.Services
{
    public class WindowsSettingsService : ISettingsService
    {
        public ISettings LoadSettings()
        {
            var appDataSettings = ApplicationData.Current.LocalSettings;
            var appSettings = new ElementarySettings();

            //Get current location in Bible, if settings are blank, set them
            //var currentTranslation = settings.Values["Translation"];
            //var currentBook = settings.Values["Book"];
            //var currentChapter = settings.Values["Chapter"];
            //var font = settings.Values["Font"];
            //var fontSize = settings.Values["FontSize"];

            //if (currentTranslation is null)
            //{
            //    settings.Values["Translation"] = "NET";
            //    currentTranslation = "NET";
            //}
            //if (currentBook is null)
            //{
            //    settings.Values["Book"] = "Genesis";
            //    currentBook = "Genesis";
            //}
            //if (currentChapter is null)
            //{
            //    settings.Values["Chapter"] = "1";
            //    currentChapter = "1";
            //}
            //if (font is null)
            //{
            //    settings.Values["Font"] = "SegoeUI";
            //    font = "SegoeUI";
            //}
            //if (fontSize is null)
            //{
            //    settings.Values["FontSize"] = "Medium";
            //    fontSize = "Medium";
            //}

            //Font = new FontFamily(font.ToString());

            //switch (fontSize.ToString())
            //{
            //    case "Small":
            //        FontSize = 16;
            //        break;
            //    case "Medium":
            //        FontSize = 18;
            //        break;
            //    case "Large":
            //        FontSize = 20;
            //        break;
            //    default:
            //        FontSize = 16;
            //        break;
            //}
            return appSettings;
        }

        public void AddOrSetSetting(string key, string value)
        {
            ApplicationData.Current.LocalSettings.Values[key] = value;
        }

        public void DeleteSetting(string key)
        {
            var setting = ApplicationData.Current.LocalSettings.Values[key];
            if (setting == null) return;
            ApplicationData.Current.LocalSettings.Values.Remove(key);
        }

        public string GetSetting(string key)
        {
            var setting = ApplicationData.Current.LocalSettings.Values[key];
            if (setting == null) return null;
            return setting.ToString();
        }
    }
}
