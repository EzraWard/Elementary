using Elementary.Core.Enums;
using Elementary.Core.Interfaces;
using Elementary.Core.Models;
using System;

namespace Elementary.Core.Services
{
    public class SettingsService : ISettingsService
    {
        private ISettingsProvider _settingsProvider;

        public void SetSettingsProvider(ISettingsProvider settingsProvider)
        {
            _settingsProvider = settingsProvider;
        }

        public ISettings GetSettings()
        {
            var appSettings = new AppSettings();

            Enum.TryParse(GetSetting("translation"), out ETranslation translation);
            appSettings.Translation = translation;

            Enum.TryParse(GetSetting("book"), out EBook book);
            appSettings.Book = book;

            appSettings.Chapter = int.Parse(GetSetting("chapter"));

            Enum.TryParse(GetSetting("font"), out EFont font);
            appSettings.Font = font;

            Enum.TryParse(GetSetting("fontSize"), out EFontSize fontSize);
            appSettings.FontSize = fontSize;

            bool.TryParse(GetSetting("showVerseNumbers"), out bool showVerseNumbers);
            appSettings.ShowVerseNumbers = showVerseNumbers;

            Enum.TryParse(GetSetting("theme"), out ETheme theme);
            appSettings.Theme = theme;

            EnsureInitialization(appSettings);
            return appSettings;
        }

        public void SaveSettings(ISettings settings)
        {
            SaveSetting("translation", settings.Translation.ToString());
            SaveSetting("book", settings.Book.ToString());
            SaveSetting("chapter", settings.Chapter.ToString());

            SaveSetting("font", settings.Font.ToString());
            SaveSetting("fontSize", settings.FontSize.ToString());
            SaveSetting("showVerseNumbers", settings.ShowVerseNumbers.ToString());
            SaveSetting("theme", settings.Theme.ToString());
        }

        private void EnsureInitialization(ISettings appSettings)
        {
            if (appSettings.Translation == ETranslation.NotSet) appSettings.Translation = ETranslation.NET;
            if (appSettings.Book == EBook.NotSet)
            {
                appSettings.Book = EBook.Genesis;
                appSettings.Chapter = 1;
            }
            if (appSettings.Chapter == 0) appSettings.Chapter = 1;

            if (appSettings.Font == EFont.NotSet) appSettings.Font = EFont.SegoeUIVariable;
            if (appSettings.FontSize == EFontSize.NotSet) appSettings.FontSize = EFontSize.Medium;

            if (appSettings.ShowVerseNumbers == null) appSettings.ShowVerseNumbers = true;

            SaveSettings(appSettings);
        }

        private string GetSetting(string key)
        {
            return _settingsProvider.GetSetting(key);
        }

        private void SaveSetting(string key, string value)
        {
            _settingsProvider.SaveSetting(key, value);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
        private void DeleteSetting(string key)
        {
            _settingsProvider.DeleteSetting(key);
        }
    }
}