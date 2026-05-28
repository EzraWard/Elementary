using Elementary.Core.Models;
using System;
using System.Collections.Generic;

namespace Elementary.Core.Interfaces
{
    public interface IReadingStreakService
    {
        event EventHandler<ReadingStreakLoggedEventArgs> ReadingActivityLogged;

        ReadingStreakProgress GetProgress();
        TimeSpan GetDailyThreshold();
        void AddReadingTime(TimeSpan readingTime, DateTime? activityDate = null);
        int GetCurrentStreak();
        int GetLongestStreak();
        IReadOnlyList<bool> GetRecentActivity(int days);
    }

    public class ReadingStreakLoggedEventArgs : EventArgs
    {
        public DateTime ActivityDate { get; set; }
        public int CurrentStreak { get; set; }
    }
}
