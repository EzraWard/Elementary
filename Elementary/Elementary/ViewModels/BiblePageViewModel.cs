using CommunityToolkit.Mvvm.ComponentModel;
using Elementary.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using VersOne.Epub;
using Windows.Storage;
using Windows.UI.ViewManagement;

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
        private string _currentBibleSelection;
        private List<string> _currentBibleBooks;
        private List<int> _currentBookChapters;
        private int _currentBookChapter;
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

        public Bible Bible { get; set; }

        public BiblePageViewModel()
        {}

        public void Initialize()
        {
            //default to NET
            _currentBibleSelection = "NET";
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
                book.Chapters = null;
            }

            _currentBookChapters = new List<int>
            {
                1
            };
            //CurrentChapterText = PrintTextContentFile(_currentBible.ReadingOrder[0]);
            //CurrentChapterText = CurrentChapterText.Replace(Environment.NewLine, " ");

            //First chapter in Genesis
            CurrentChapterContent = _currentBible.ReadingOrder[3].Content;
        }
    }
}
