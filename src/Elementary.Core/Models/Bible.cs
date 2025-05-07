using System.Collections.ObjectModel;

namespace Elementary.Core.Models
{
    public class Bible
    {
        public ObservableCollection<Book> Books;

        public Bible()
        {
            Books = new ObservableCollection<Book>();
        }
    }

    public class Book
    {
        public string Title { get; set; }
        public int ReadingOrderIndex { get; set; }
        public ObservableCollection<Chapter> Chapters { get; set; }

        public Book() { }
    }

    public class Chapter
    {
        public int Index { get; set; }
        public string ChapterText { get; set; }

        public Chapter() { }
    }
}
