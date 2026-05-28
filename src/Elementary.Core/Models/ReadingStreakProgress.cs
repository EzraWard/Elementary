using System;
using System.Collections.Generic;

namespace Elementary.Core.Models
{
    public class ReadingStreakProgress
    {
        public List<DateTime> ActiveDates { get; set; } = new List<DateTime>();
        public Dictionary<DateTime, int> DailyReadingSeconds { get; set; } = new Dictionary<DateTime, int>();
    }
}
