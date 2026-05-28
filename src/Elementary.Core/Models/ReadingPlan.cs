using System.Collections.Generic;

namespace Elementary.Core.Models
{
    public class ReadingPlan
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<ReadingPlanDay> Days { get; set; } = new List<ReadingPlanDay>();

        public int TotalDays => Days?.Count ?? 0;
        public string TotalDaysText => $"{TotalDays} days";
    }
}
