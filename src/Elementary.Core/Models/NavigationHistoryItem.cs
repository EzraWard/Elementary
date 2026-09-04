using System;

namespace Elementary.Core.Models
{
    public class NavigationHistoryItem
    {
        public string BookTitle { get; set; }
        public int Chapter { get; set; }
        public string BookKey { get; set; }

        public string DisplayText => $"{(string.IsNullOrWhiteSpace(BookTitle) ? BookKey : BookTitle)} {Chapter}";

        public override string ToString()
        {
            return DisplayText;
        }
    }
}
