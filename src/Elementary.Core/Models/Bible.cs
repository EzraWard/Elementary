using System.Collections.ObjectModel;

namespace Elementary.Core.Models
{
    public enum ChapterDisplayLineType
    {
        Verse,
        Heading,
        Poetry,
        ParagraphBreak,
        Footnote,
        Text
    }

    public class ChapterDisplayLine
    {
        public ChapterDisplayLineType Type { get; set; }
        public int VerseNumber { get; set; }
        public string Text { get; set; }

        public bool IsVerse => Type == ChapterDisplayLineType.Verse;
        public bool IsHeading => Type == ChapterDisplayLineType.Heading;
        public bool IsPoetry => Type == ChapterDisplayLineType.Poetry;
        public bool IsFootnote => Type == ChapterDisplayLineType.Footnote;
        public bool IsParagraphBreak => Type == ChapterDisplayLineType.ParagraphBreak;
        public bool IsText => Type == ChapterDisplayLineType.Text;
        public string VerseNumberText => VerseNumber > 0 ? VerseNumber.ToString() : string.Empty;
    }

    public class Bible
    {
        public ObservableCollection<Book> Books { get; set; }

        public Bible()
        {
            Books = new ObservableCollection<Book>();
        }
    }

    public class Book
    {
        public string Title { get; set; }
        public int ReadingOrderIndex { get; set; }
        public int ChapterCount { get; set; }
        public string SourcePath { get; set; }
        public bool IsChaptersLoaded { get; set; }
        public ObservableCollection<Chapter> Chapters { get; set; }

        public Book() { }
    }

    public class Chapter
    {
        public int Index { get; set; }
        public string ChapterText { get; set; }
        public ObservableCollection<ChapterDisplayLine> DisplayLines { get; set; } = new ObservableCollection<ChapterDisplayLine>();

        public Chapter() { }
    }
}
