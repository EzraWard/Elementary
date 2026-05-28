using Elementary.Core.Enums;
using Elementary.Core.Extensions;

namespace Elementary.Core.Models
{
    public class ReadingPlanPassage
    {
        public EBook Book { get; set; }
        public int Chapter { get; set; }

        public string BookTitle => Book.GetDisplayName();
        public string BookKey => Book.ToString();
        public string ReferenceText => $"{BookTitle} {Chapter}";
    }
}
