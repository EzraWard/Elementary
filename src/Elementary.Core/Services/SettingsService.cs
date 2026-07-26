using Elementary.Core.Enums;
using Elementary.Core.Interfaces;
using Elementary.Core.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Elementary.Core.Services
{
    public class SettingsService : ISettingsService
    {
        private ISettingsProvider _settingsProvider;

        public SettingsService(ISettingsProvider settingsProvider)
        {
            _settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
        }

        public ISettings GetSettings()
        {
            var appSettings = new AppSettings();

            var translationS = GetSetting("translation");

            Enum.TryParse(translationS, out ETranslation translation);
            appSettings.Translation = translation;

            Enum.TryParse(GetSetting("book"), out EBook book);
            appSettings.Book = book;

            int.TryParse(GetSetting("chapter"), out int chapter);
            appSettings.Chapter = chapter;

            Enum.TryParse(GetSetting("font"), out EFont font);
            appSettings.Font = font;

            Enum.TryParse(GetSetting("fontSize"), out EFontSize fontSize);
            appSettings.FontSize = fontSize;

            bool? showVerseNumbers = null;
            var showVerseNumbersStr = GetSetting("showVerseNumbers");
            if (!string.IsNullOrWhiteSpace(showVerseNumbersStr))
            {
                if (bool.TryParse(showVerseNumbersStr, out bool parsed))
                    showVerseNumbers = parsed;
            }
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

            if (appSettings.ShowVerseNumbers == null)
                appSettings.ShowVerseNumbers = true;

            if (appSettings.Theme == ETheme.NotSet) appSettings.Theme = ETheme.System;

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

        // Navigation history handling (manual book/chapter selections only)
        public System.Collections.Generic.List<NavigationHistoryItem> GetNavigationHistory()
        {
            var raw = GetSetting("navigationHistory");
            var list = new System.Collections.Generic.List<NavigationHistoryItem>();
            if (string.IsNullOrWhiteSpace(raw)) return list;

            var parts = raw.Split(new char[] {';'}, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                var pieces = p.Split(new char[] {'|'});
                if (pieces.Length >= 2)
                {
                    if (int.TryParse(pieces[1], out int chap))
                    {
                        list.Add(new NavigationHistoryItem
                        {
                            BookTitle = pieces[0],
                            Chapter = chap,
                            BookKey = pieces.Length >= 3 ? pieces[2] : null
                        });
                    }
                }
            }
            return list;
        }

        public void SaveNavigationHistory(System.Collections.Generic.List<NavigationHistoryItem> history)
        {
            if (history == null) history = new System.Collections.Generic.List<NavigationHistoryItem>();
            // Trim to 10 (oldest first)
            if (history.Count > 10)
            {
                history = history.GetRange(history.Count - 10, 10);
            }
            var parts = new System.Collections.Generic.List<string>();
            foreach (var h in history)
            {
                if (string.IsNullOrWhiteSpace(h.BookKey))
                {
                    parts.Add($"{h.BookTitle}|{h.Chapter}");
                    continue;
                }

                parts.Add($"{h.BookTitle}|{h.Chapter}|{h.BookKey}");
            }
            SaveSetting("navigationHistory", string.Join(";", parts));
        }

        public ReadingStreakProgress GetReadingStreakProgress()
        {
            var raw = GetSetting("readingStreak");
            var progress = new ReadingStreakProgress();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return progress;
            }

            var segments = raw.Split(new[] { ';' }, StringSplitOptions.None);
            var serializedDates = (segments.Length > 0 ? segments[0] : string.Empty)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var serializedDate in serializedDates)
            {
                if (DateTime.TryParseExact(serializedDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var activeDate))
                {
                    progress.ActiveDates.Add(activeDate.Date);
                }
            }

            progress.ActiveDates = progress.ActiveDates
                .Distinct()
                .OrderBy(date => date)
                .ToList();

            if (segments.Length > 1 && !string.IsNullOrWhiteSpace(segments[1]))
            {
                var serializedReadingTimes = segments[1].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var serializedReadingTime in serializedReadingTimes)
                {
                    var parts = serializedReadingTime.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 2)
                    {
                        continue;
                    }

                    if (!DateTime.TryParseExact(parts[0], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var readingDate))
                    {
                        continue;
                    }

                    if (!int.TryParse(parts[1], out var readingSeconds))
                    {
                        continue;
                    }

                    progress.DailyReadingSeconds[readingDate.Date] = Math.Max(0, readingSeconds);
                }
            }

            return progress;
        }

        public void SaveReadingStreakProgress(ReadingStreakProgress progress)
        {
            if (progress == null)
            {
                SaveSetting("readingStreak", string.Empty);
                return;
            }

            var serializedDates = string.Join(",",
                (progress.ActiveDates ?? new List<DateTime>())
                    .Select(date => date.Date)
                    .Distinct()
                    .OrderBy(date => date)
                    .Select(date => date.ToString("yyyyMMdd", CultureInfo.InvariantCulture)));

            var serializedReadingSeconds = string.Join(",",
                (progress.DailyReadingSeconds ?? new Dictionary<DateTime, int>())
                    .Where(entry => entry.Value > 0)
                    .OrderBy(entry => entry.Key.Date)
                    .Select(entry => $"{entry.Key.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}={entry.Value}"));

            if (string.IsNullOrWhiteSpace(serializedDates) && string.IsNullOrWhiteSpace(serializedReadingSeconds))
            {
                SaveSetting("readingStreak", string.Empty);
                return;
            }

            SaveSetting("readingStreak", $"{serializedDates};{serializedReadingSeconds}");
        }

    }
}
