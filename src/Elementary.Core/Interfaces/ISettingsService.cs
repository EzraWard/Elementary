using System.Collections.Generic;
using Elementary.Core.Models;

namespace Elementary.Core.Interfaces
{
    public interface ISettingsService
    {
        ISettings GetSettings();

        void SaveSettings(ISettings settings);

        // Recently departed reading locations (up to 10 items)
        List<NavigationHistoryItem> GetNavigationHistory();
        void SaveNavigationHistory(List<NavigationHistoryItem> history);

        ReadingStreakProgress GetReadingStreakProgress();
        void SaveReadingStreakProgress(ReadingStreakProgress progress);
    }
}
