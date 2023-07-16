using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elementary.Objects
{
    public class Bible
    {
        public string Title { get; set; }
        public List<Book> Books;

        public Bible()
        {
            Books = new List<Book>();
        }
    }

    public class Book
    {
        public string Title { get; set; }
        public List<Chapter> Chapters { get; set; }

        public Book() { }
    }

    public class Chapter
    {
        public int Index { get; set; }
        public int ReadingOrderIndex { get; set; }

        public Chapter() { }
    }
}
