using CommunityToolkit.Mvvm.ComponentModel;
using Elementary.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;

namespace Elementary.ViewModels
{
    public class StreakPageViewModel : ObservableObject
    {
        private const int RecentActivityDays = 7;
        private readonly IReadingStreakService _readingStreakService;
        private readonly ObservableCollection<StreakDayActivityViewModel> _recentActivity = new ObservableCollection<StreakDayActivityViewModel>();

        public StreakPageViewModel()
        {
            _readingStreakService = App.Services.GetRequiredService<IReadingStreakService>();
        }

        public ObservableCollection<StreakDayActivityViewModel> RecentActivity => _recentActivity;

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
                ? $"Your streak grows when you read for at least {ThresholdText} on consecutive calendar days."
                : $"Read for {ThresholdText} in the Bible today to start your streak.";

        public string ThresholdHelpText => $"A day counts once after you reach {ThresholdText} of reading.";

        public string HowItWorksText =>
            $"Read for at least {ThresholdText} on consecutive calendar days to grow the streak. Missing a full day resets the current streak, but your longest streak stays.";

        public void Initialize()
        {
            Refresh();
        }

        public void Refresh()
        {
            ReplaceRecentActivity();
            OnPropertyChanged(nameof(CurrentStreak));
            OnPropertyChanged(nameof(LongestStreak));
            OnPropertyChanged(nameof(HasActivity));
            OnPropertyChanged(nameof(StreakLabel));
            OnPropertyChanged(nameof(ThresholdText));
            OnPropertyChanged(nameof(SummaryText));
            OnPropertyChanged(nameof(ThresholdHelpText));
            OnPropertyChanged(nameof(HowItWorksText));
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
    }

    public class StreakDayActivityViewModel
    {
        public bool Active { get; set; }
        public DateTime Date { get; set; }
        public string DayOfWeekShort => Date.ToString("ddd");
    }
}
