using CommunityToolkit.Mvvm.ComponentModel;
using Elementary.Core.Interfaces;
using Elementary.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Elementary.ViewModels
{
    public class StreakPageViewModel : ObservableObject
    {
        private const int RecentActivityDays = 7;
        private readonly IReadingStreakService _readingStreakService;
        private readonly ObservableCollection<StreakDayActivityViewModel> _recentActivity = new ObservableCollection<StreakDayActivityViewModel>();
        private readonly ObservableCollection<StreakBadgeProgress> _badges = new ObservableCollection<StreakBadgeProgress>();

        public StreakPageViewModel() : this(App.Services.GetRequiredService<IReadingStreakService>())
        {
        }

        internal StreakPageViewModel(IReadingStreakService readingStreakService)
        {
            _readingStreakService = readingStreakService ?? throw new ArgumentNullException(nameof(readingStreakService));
        }

        public ObservableCollection<StreakDayActivityViewModel> RecentActivity => _recentActivity;

        public ObservableCollection<StreakBadgeProgress> Badges => _badges;

        public int CurrentStreak => _readingStreakService.GetCurrentStreak();

        public int LongestStreak => _readingStreakService.GetLongestStreak();

        public bool HasActivity => LongestStreak > 0;

        public string StreakLabel => "day streak";

        public string ThresholdText
        {
            get
            {
                var threshold = _readingStreakService.GetDailyThreshold();
                return threshold.TotalMinutes >= 1 && Math.Abs(threshold.TotalSeconds % 60) < double.Epsilon
                    ? $"{(int)threshold.TotalMinutes} minute{(threshold.TotalMinutes == 1 ? string.Empty : "s")}"
                    : $"{(int)threshold.TotalSeconds} second{(threshold.TotalSeconds == 1 ? string.Empty : "s")}";
            }
        }

        public string SummaryText =>
            HasActivity
                ? $"Your streak grows when you actively read the Bible for at least {ThresholdText} on consecutive calendar days."
                : $"Actively read the Bible for {ThresholdText} today to start your streak.";

        public string ThresholdHelpText => $"A day counts after {ThresholdText} with the Bible open and Elementary active.";

        public string HowItWorksText =>
            $"Open the Bible, settle in, and read for {ThresholdText} each day. Come back tomorrow to keep the flame glowing! If you miss a day, your current streak starts fresh, but your best streak stays safe.";

        public string BadgeSummaryText
        {
            get
            {
                var nextBadge = _badges.FirstOrDefault(badge => badge.IsNextToEarn);
                return nextBadge == null
                    ? "You've unlocked every current streak badge."
                    : $"Next badge: {nextBadge.Title} at {nextBadge.ThresholdDays} day{(nextBadge.ThresholdDays == 1 ? string.Empty : "s")}.";
            }
        }

        public void Initialize()
        {
            Refresh();
        }

        public void Refresh()
        {
            ReplaceRecentActivity();
            ReplaceBadges();
            OnPropertyChanged(nameof(CurrentStreak));
            OnPropertyChanged(nameof(LongestStreak));
            OnPropertyChanged(nameof(HasActivity));
            OnPropertyChanged(nameof(StreakLabel));
            OnPropertyChanged(nameof(ThresholdText));
            OnPropertyChanged(nameof(SummaryText));
            OnPropertyChanged(nameof(ThresholdHelpText));
            OnPropertyChanged(nameof(HowItWorksText));
            OnPropertyChanged(nameof(BadgeSummaryText));
        }

        private void ReplaceRecentActivity()
        {
            _recentActivity.Clear();
            var currentDate = DateTime.Today.AddDays((RecentActivityDays - 1) * -1);
            foreach (var isActive in _readingStreakService.GetRecentActivity(RecentActivityDays))
            {
                _recentActivity.Add(new StreakDayActivityViewModel
                {
                    Active = isActive,
                    Date = currentDate
                });

                currentDate = currentDate.AddDays(1);
            }
        }

        private void ReplaceBadges()
        {
            _badges.Clear();
            foreach (var badge in StreakBadgeCatalog.BuildProgress(LongestStreak))
            {
                _badges.Add(badge);
            }
        }
    }

    public class StreakDayActivityViewModel
    {
        public bool Active { get; set; }
        public DateTime Date { get; set; }
        public string DayOfWeekShort => Date.ToString("ddd");
    }
}
