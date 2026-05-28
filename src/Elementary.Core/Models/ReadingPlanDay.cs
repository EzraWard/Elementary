using System.Collections.Generic;
using System.Linq;

namespace Elementary.Core.Models
{
    public class ReadingPlanDay
    {
        public int DayNumber { get; set; }
        public List<ReadingPlanPassage> Passages { get; set; } = new List<ReadingPlanPassage>();

        public string Summary => string.Join(", ", Passages.Select(passage => passage.ReferenceText));
    }
}
