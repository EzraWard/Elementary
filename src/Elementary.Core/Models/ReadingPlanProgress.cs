namespace Elementary.Core.Models
{
    public class ReadingPlanProgress
    {
        public string ActivePlanId { get; set; }
        public int CompletedDayCount { get; set; }

        public bool HasActivePlan => !string.IsNullOrWhiteSpace(ActivePlanId);
    }
}
