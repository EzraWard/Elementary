using System.Collections.Generic;
using Elementary.Core.Models;

namespace Elementary.Core.Interfaces
{
    public interface ISettingsService
    {
        ISettings GetSettings();

        void SaveSettings(ISettings settings);

        // Navigation history for manual book/chapter selections (up to 10 items)
        List<NavigationHistoryItem> GetNavigationHistory();
        void SaveNavigationHistory(List<NavigationHistoryItem> history);
    }
}