using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elementary.Objects
{
    public class Bible
    {
        public string Title { get; set; }
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
        public int ReadingOrderIndex { get; set; }

        public Chapter() { }
    }
}
