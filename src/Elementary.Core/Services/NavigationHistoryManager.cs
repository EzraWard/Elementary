using Elementary.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Elementary.Core.Services
{
    public static class NavigationHistoryManager
    {
        private const int MaximumHistoryItems = 10;

        public static List<NavigationHistoryItem> RecordDeparture(
            IEnumerable<NavigationHistoryItem> history,
            NavigationHistoryItem departedLocation,
            NavigationHistoryItem currentLocation)
        {
            var updatedHistory = (history ?? Enumerable.Empty<NavigationHistoryItem>())
                .Where(item => item != null)
                .ToList();

            if (currentLocation != null)
            {
                updatedHistory.RemoveAll(item => AreSameLocation(item, currentLocation));
            }

            if (departedLocation != null && !AreSameLocation(departedLocation, currentLocation))
            {
                updatedHistory.RemoveAll(item => AreSameLocation(item, departedLocation));
                updatedHistory.Add(new NavigationHistoryItem
                {
                    BookTitle = departedLocation.BookTitle,
                    BookKey = departedLocation.BookKey,
                    Chapter = departedLocation.Chapter
                });
            }

            return updatedHistory.Count <= MaximumHistoryItems
                ? updatedHistory
                : updatedHistory.Skip(updatedHistory.Count - MaximumHistoryItems).ToList();
        }

        public static bool AreSameLocation(NavigationHistoryItem first, NavigationHistoryItem second)
        {
            if (first == null || second == null || first.Chapter != second.Chapter)
            {
                return false;
            }

            var keysMatch = !string.IsNullOrWhiteSpace(first.BookKey)
                            && !string.IsNullOrWhiteSpace(second.BookKey)
                            && string.Equals(first.BookKey, second.BookKey, StringComparison.OrdinalIgnoreCase);
            var titlesMatch = !string.IsNullOrWhiteSpace(first.BookTitle)
                              && !string.IsNullOrWhiteSpace(second.BookTitle)
                              && string.Equals(first.BookTitle, second.BookTitle, StringComparison.OrdinalIgnoreCase);

            return keysMatch || titlesMatch;
        }
    }
}
