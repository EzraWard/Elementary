namespace Elementary.Core.Models
{
    public class SearchResult
    {
        public string BookTitle { get; set; }
        public string BookKey { get; set; }
        public int ChapterIndex { get; set; }
        public int VerseNumber { get; set; }
        public string VerseText { get; set; }

        public string ReferenceText => $"{BookTitle} {ChapterIndex}:{VerseNumber}";
    }
}
