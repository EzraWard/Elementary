using CommunityToolkit.Mvvm.ComponentModel;
using Elementary.Objects;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using VersOne.Epub;
using Windows.ApplicationModel.Appointments.AppointmentsProvider;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace Elementary.ViewModels
{
    public partial class BiblePageViewModel : ObservableObject
    {
        private static readonly Dictionary<string, string> BibleDictionary = new Dictionary<string, string>
        {
            { "NET", "ms-appx:///Content/NET21NOTELESS.epub" },
            { "KJV", "ms-appx:///Content/KJVNoImages.epub"  },
            { "ASV", "ms-appx:///Content/eng-asv.epub"  }
        };

        private EpubBook _currentBible;
        private Bible _bible;
        private Book _book;
        private Chapter _chapter;
        private List<string> _currentBibleBooks;
        private List<int> _currentBookChapters;
        private string _currentChapterText;
        private string _currentChapterContent;

        public EpubBook CurrentBible { 
            get 
            { 
                return _currentBible; 
            } 
            set 
            { 
                _currentBible = value; 
            } 
        }

        public List<string> CurrentBibleBooks
        {
            get => _currentBibleBooks;
            set => SetProperty( ref _currentBibleBooks, value);
        }

        public List<int> CurrentBookChapters
        {
            get => _currentBookChapters;
            set => SetProperty(ref _currentBookChapters, value);
        }

        public string CurrentChapterText
        {
            get => _currentChapterText;
            set => SetProperty(ref _currentChapterText, value);
        }

        public string CurrentChapterContent
        {
            get => _currentChapterContent;
            set => SetProperty(ref _currentChapterContent, value);
        }

        public Bible Bible
        {
            get => _bible;
            set => SetProperty(ref _bible, value);
        }

        public Book Book
        {
            get => _book;
            set => SetProperty(ref _book, value);
        }

        public Chapter Chapter
        {
            get => _chapter;
            set => SetProperty(ref _chapter, value);
        }

        public BiblePageViewModel()
        {}

        public void Initialize()
        {
            //default to NET
            var biblePath = BibleDictionary["NET"];
            var bibleFilePath = StorageFile.GetFileFromApplicationUriAsync(new Uri(biblePath)).AsTask().Result.Path;
            
            _currentBible = EpubReader.ReadBook(bibleFilePath);

            //Enumerate Books
            _currentBibleBooks = new List<string>();
            Bible = new Bible();
            foreach(var book in _currentBible.Navigation)
            {
                Bible.Books.Add(new Book
                {
                    Title = book.Title
                });
            }
            foreach (var book in Bible.Books)
            {
                book.ReadingOrderIndex = GetStartingPointofBook(book);
            }
            for(int i = 0; i < Bible.Books.Count; i++) 
            {
                int numberOfChapters;
                if (Bible.Books[i].Title != "Revelation")
                {
                    numberOfChapters = Bible.Books[i + 1].ReadingOrderIndex - Bible.Books[i].ReadingOrderIndex;
                }
                else
                {
                    numberOfChapters = 22;
                }

                Bible.Books[i].Chapters = new ObservableCollection<Chapter>();
                for (int  j = 1; j < numberOfChapters; j++) 
                {
                    Bible.Books[i].Chapters.Add(new Chapter { Index = j, ReadingOrderIndex = Bible.Books[i].ReadingOrderIndex + j});
                }
            }

            Book = Bible.Books[0];
            Chapter = Book.Chapters[0];

            //First chapter in Genesis

            //var content = _currentBible.ReadingOrder[Chapter.ReadingOrderIndex].Content;
            //var match = Regex.Match(content, "(.*<\\s* body[^>]*>)| (<\\s */\\s* body\\s *\\>.+)");
            var htmlDoc = new HtmlDocument();
            htmlDoc.OptionWriteEmptyNodes = true;
            htmlDoc.LoadHtml(_currentBible.ReadingOrder[Chapter.ReadingOrderIndex].Content);
            //foreach (var brTag in htmlDoc.DocumentNode.SelectNodes("//br"))
            //    brTag.Remove();
            CurrentChapterContent = htmlDoc.DocumentNode.SelectSingleNode("//body").InnerHtml;
        }

        public void SetCurrentChapterContent(int readingOrderIndex)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.OptionWriteEmptyNodes = true;
            htmlDoc.LoadHtml(_currentBible.ReadingOrder[readingOrderIndex].Content);
            //foreach (var brTag in htmlDoc.DocumentNode.SelectNodes("//br"))
            //    brTag.Remove();
            var test = htmlDoc.DocumentNode.SelectSingleNode("//body").InnerHtml;
            CurrentChapterContent = test;
        }

        private int GetStartingPointofBook(Book book)
        {
            switch (book.Title)
            {
                case "Genesis":
                    return 2;                    
                case "Exodus":
                    return 53;
                case "Leviticus":
                    return 94;
                case "Numbers":
                    return 122;
                case "Deuteronomy":
                    return 159;
                case "Joshua":
                    return 194;
                case "Judges":
                    return 219;
                case "Ruth":
                    return 234 + 7;
                case "1 Samuel":
                    return 238 + 8;
                case "2 Samuel":
                    return 269 + 9;
                case "1 Kings":
                    return 293 + 10;
                case "2 Kings":
                    return 315 + 11;
                case "1 Chronicles":
                    return 340 + 12;
                case "2 Chronicles":
                    return 369 + 13;
                case "Ezra":
                    return 405 + 14;
                case "Nehemiah":
                    return 415 + 15;
                case "Esther":
                    return 428 + 16;
                case "Job":
                    return 438 + 17;
                case "Psalms":
                    return 480 + 18;
                case "Proverbs":
                    return 630 + 19;
                case "Ecclesiastes":
                    return 661 + 20;
                case "Song of Solomon":
                    return 673 + 21;
                case "Isaiah":
                    return 681 + 22;
                case "Jeremiah":
                    return 747 + 23;
                case "Lamentations":
                    return 799 + 24;
                case "Ezekiel":
                    return 804 + 25;
                case "Daniel":
                    return 852 + 26;
                case "Hosea":
                    return 864 + 27;
                case "Joel":
                    return 878 + 28;
                case "Amos":
                    return 881 + 29;
                case "Obadiah":
                    return 890 + 30;
                case "Jonah":
                    return 891 + 31;
                case "Micah":
                    return 895 + 32;
                case "Nahum":
                    return 902 + 33;
                case "Habakkuk":
                    return 905 + 34;
                case "Zephaniah":
                    return 908 + 35;
                case "Haggai":
                    return 911 + 36;
                case "Zechariah":
                    return 913 + 37;
                case "Malachi":
                    return 927 + 38;
                case "Matthew":
                    return 931 + 39;
                case "Mark":
                    return 959 + 40;
                case "Luke":
                    return 975 + 41;
                case "John":
                    return 999 + 42;
                case "Acts":
                    return 1020 + 43;
                case "Romans":
                    return 1048 + 44;
                case "1 Corinthians":
                    return 1064 + 45;
                case "2 Corinthians":
                    return 1080 + 46;
                case "Galatians":
                    return 1093 + 47;
                case "Ephesians":
                    return 1099 + 48;
                case "Philippians":
                    return 1105 + 49;
                case "Colossians":
                    return 1109 + 50;
                case "1 Thessalonians":
                    return 1113 + 51;
                case "2 Thessalonians":
                    return 1118 + 52;
                case "1 Timothy":
                    return 1121 + 53;
                case "2 Timothy":
                    return 1127 + 54;
                case "Titus":
                    return 1131 + 55;
                case "Philemon":
                    return 1134 + 56;
                case "Hebrews":
                    return 1135 + 57;
                case "James":
                    return 1148 + 58;
                case "1 Peter":
                    return 1153 + 59;
                case "2 Peter":
                    return 1158 + 60;
                case "1 John":
                    return 1161 + 61;
                case "2 John":
                    return 1166 + 62;
                case "3 John":
                    return 1167 + 63;
                case "Jude":
                    return 1167 + 65;
                case "Revelation":
                    return 1169 + 65;
                default:
                    return 0;
            }
        }
    }
}
