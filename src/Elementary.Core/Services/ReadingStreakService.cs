using Elementary.Core.Interfaces;
using Elementary.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Elementary.Core.Services
{
    public class ReadingStreakService : IReadingStreakService
    {
#if DEBUG
        private const int DailyStreakThresholdSeconds = 10;
#else
        private const int DailyStreakThresholdSeconds = 600;
#endif
        private readonly ISettingsService _settingsService;

        public event EventHandler<ReadingStreakLoggedEventArgs> ReadingActivityLogged;

        public ReadingStreakService(ISettingsService settingsService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        }

        public ReadingStreakProgress GetProgress()
        {
            var progress = _settingsService.GetReadingStreakProgress() ?? new ReadingStreakProgress();
            progress.ActiveDates = NormalizeDates(progress.ActiveDates);
            progress.DailyReadingSeconds = NormalizeReadingSeconds(progress.DailyReadingSeconds);
            return progress;
        }

        public TimeSpan GetDailyThreshold()
        {
            return TimeSpan.FromSeconds(DailyStreakThresholdSeconds);
        }

        public void AddReadingTime(TimeSpan readingTime, DateTime? activityDate = null)
        {
            var secondsToAdd = Math.Max(0, (int)Math.Floor(readingTime.TotalSeconds));
            if (secondsToAdd == 0)
            {
                return;
            }

            var date = (activityDate ?? DateTime.Today).Date;
            var progress = GetProgress();
            progress.DailyReadingSeconds.TryGetValue(date, out var currentSeconds);
            var updatedSeconds = currentSeconds + secondsToAdd;
            progress.DailyReadingSeconds[date] = updatedSeconds;

            var streakActivated = !progress.ActiveDates.Contains(date) && updatedSeconds >= DailyStreakThresholdSeconds;
            if (streakActivated)
            {
                progress.ActiveDates.Add(date);
                progress.ActiveDates = NormalizeDates(progress.ActiveDates);
            }

            _settingsService.SaveReadingStreakProgress(progress);

            if (streakActivated)
            {
                ReadingActivityLogged?.Invoke(this, new ReadingStreakLoggedEventArgs
                {
                    ActivityDate = date,
                    CurrentStreak = GetCurrentStreak()
                });
            }
        }

        public int GetCurrentStreak()
        {
            var activeDates = NormalizeDates(GetProgress().ActiveDates)
                .OrderByDescending(date => date)
                .ToList();
            if (activeDates.Count == 0)
            {
                return 0;
            }

            var latestDate = activeDates[0];
            if (DateTime.Today > latestDate.AddDays(1))
            {
                return 0;
            }

            var streak = 1;
            var expectedDate = latestDate.AddDays(-1);
            for (var i = 1; i < activeDates.Count; i++)
            {
                if (activeDates[i] == expectedDate)
                {
                    streak++;
                    expectedDate = expectedDate.AddDays(-1);
                }
                else if (activeDates[i] < expectedDate)
                {
                    break;
                }
            }

            return streak;
        }

        public int GetLongestStreak()
        {
            var activeDates = NormalizeDates(GetProgress().ActiveDates);
            if (activeDates.Count == 0)
            {
                return 0;
            }

            var longest = 1;
            var current = 1;
            for (var i = 1; i < activeDates.Count; i++)
            {
                if (activeDates[i] == activeDates[i - 1].AddDays(1))
                {
                    current++;
                }
                else
                {
                    current = 1;
                }

                if (current > longest)
                {
                    longest = current;
                }
            }

            return longest;
        }

        public IReadOnlyList<bool> GetRecentActivity(int days)
        {
            if (days <= 0)
            {
                return Array.Empty<bool>();
            }

            var activeDates = new HashSet<DateTime>(NormalizeDates(GetProgress().ActiveDates));
            var result = new List<bool>(days);
            var currentDate = DateTime.Today.AddDays((days - 1) * -1);
            for (var i = 0; i < days; i++)
            {
                result.Add(activeDates.Contains(currentDate));
                currentDate = currentDate.AddDays(1);
            }

            return result;
        }

        private static List<DateTime> NormalizeDates(IEnumerable<DateTime> dates)
        {
            return (dates ?? Enumerable.Empty<DateTime>())
                .Select(date => date.Date)
                .Distinct()
                .OrderBy(date => date)
                .ToList();
        }

        private static Dictionary<DateTime, int> NormalizeReadingSeconds(IDictionary<DateTime, int> dailyReadingSeconds)
        {
            return (dailyReadingSeconds ?? new Dictionary<DateTime, int>())
                .GroupBy(entry => entry.Key.Date)
                .ToDictionary(
                    group => group.Key,
                    group => Math.Max(0, group.Max(entry => entry.Value)));
        }
    }
}
