using System;

namespace Elementary.Core.Models
{
    public class NavigationHistoryItem
    {
        public string BookTitle { get; set; }
        public int Chapter { get; set; }

        public override string ToString()
        {
            return $"{BookTitle} {Chapter}";
        }
    }
}