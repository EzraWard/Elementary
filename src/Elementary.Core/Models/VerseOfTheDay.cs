using System;

namespace Elementary.Models
{
    public class VerseOfTheDay
    {
        public DateTime Date { get; set; }
        public string ImageUrl { get; set; }
        public string FormattedDate => Date.ToShortDateString();
        public string Title => $"Verse of the Day for {FormattedDate}";
    }
}