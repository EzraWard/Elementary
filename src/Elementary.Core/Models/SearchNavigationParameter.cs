namespace Elementary.Core.Models
{
    public class SearchNavigationParameter
    {
        public string BookTitle { get; set; }
        public string BookKey { get; set; }
        public int ChapterIndex { get; set; }
        public int VerseNumber { get; set; }
        public string SearchQuery { get; set; }
    }
}
