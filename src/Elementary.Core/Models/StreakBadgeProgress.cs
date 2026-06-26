using System;
using System.Collections.Generic;

namespace Elementary.Core.Models
{
    public class StreakBadgeProgress
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PlaceholderText { get; set; } = string.Empty;
        public int ThresholdDays { get; set; }
        public bool IsEarned { get; set; }
        public bool IsNextToEarn { get; set; }

        public double PlaceholderFontSize => PlaceholderText.Length >= 4 ? 14 : 18;

        public string StatusText =>
            IsEarned
                ? "Unlocked"
                : IsNextToEarn
                    ? $"Next up at {ThresholdDays} day{(ThresholdDays == 1 ? string.Empty : "s")}"
                    : $"Locked until {ThresholdDays} days";
    }

    public static class StreakBadgeCatalog
    {
        private static readonly IReadOnlyList<StreakBadgeDefinition> Definitions = new[]
        {
            new StreakBadgeDefinition("first-day", "First day", "Read enough in one day to start your streak.", "1D", 1),
            new StreakBadgeDefinition("one-week", "One week", "Keep your streak alive for seven straight days.", "7D", 7),
            new StreakBadgeDefinition("two-weeks", "Two weeks", "Stay consistent for a full two-week run.", "14D", 14),
            new StreakBadgeDefinition("one-month", "One month", "Read every day for thirty straight days.", "30D", 30),
            new StreakBadgeDefinition("centurion", "Centurion", "Reach a one-hundred-day streak.", "100D", 100),
            new StreakBadgeDefinition("one-year", "One year", "Keep the streak alive for a full year.", "365D", 365)
        };

        public static IReadOnlyList<StreakBadgeProgress> BuildProgress(int longestStreak)
        {
            var normalizedLongest = Math.Max(0, longestStreak);
            var badgeProgress = new List<StreakBadgeProgress>(Definitions.Count);
            var nextBadgeAssigned = false;

            foreach (var definition in Definitions)
            {
                var isEarned = normalizedLongest >= definition.ThresholdDays;
                var isNextToEarn = !isEarned && !nextBadgeAssigned;
                if (isNextToEarn)
                {
                    nextBadgeAssigned = true;
                }

                badgeProgress.Add(new StreakBadgeProgress
                {
                    Id = definition.Id,
                    Title = definition.Title,
                    Description = definition.Description,
                    PlaceholderText = definition.PlaceholderText,
                    ThresholdDays = definition.ThresholdDays,
                    IsEarned = isEarned,
                    IsNextToEarn = isNextToEarn
                });
            }

            return badgeProgress;
        }

        private sealed class StreakBadgeDefinition
        {
            public StreakBadgeDefinition(string id, string title, string description, string placeholderText, int thresholdDays)
            {
                Id = id;
                Title = title;
                Description = description;
                PlaceholderText = placeholderText;
                ThresholdDays = thresholdDays;
            }

            public string Id { get; }
            public string Title { get; }
            public string Description { get; }
            public string PlaceholderText { get; }
            public int ThresholdDays { get; }
        }
    }
}
