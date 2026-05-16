namespace Elementary.VerseOfTheDay.Models
{
    public class BibleVerseData
    {
        public string VerseText { get; set; } = string.Empty;
        public string Book { get; set; } = string.Empty;
        public string Chapter { get; set; } = string.Empty;
        public string Verse { get; set; } = string.Empty;

        public string Reference => $"{Book} {Chapter}:{Verse}";
    }
}
