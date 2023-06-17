using CommunityToolkit.Mvvm.ComponentModel;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using VersOne.Epub;
using Windows.Storage;

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
        public string CurrentChapterText
        {
            get => _currentChapterText;
            set => SetProperty(ref _currentChapterText, value);
        }

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
            foreach(var book in _currentBible.Navigation)
            {
                _currentBibleBooks.Add(book.Title);
            }
            _currentBookChapters = new List<int>
            {
                1
            };
            CurrentChapterText = PrintTextContentFile(_currentBible.ReadingOrder[20]);
            CurrentChapterText = CurrentChapterText.Replace(Environment.NewLine, " ");
        }

        private string PrintTextContentFile(EpubLocalTextContentFile textContentFile)
        {
            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(textContentFile.Content);
            StringBuilder sb = new StringBuilder();
            foreach (HtmlNode node in htmlDocument.DocumentNode.SelectNodes("//text()"))
            {
                sb.AppendLine(node.InnerText.Trim());
            }
            return sb.ToString();
        }
    }
}
