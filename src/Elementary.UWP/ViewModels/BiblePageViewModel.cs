using CommunityToolkit.Mvvm.ComponentModel;
using Elementary.Core.Interfaces;
using Elementary.Core.Models;
using Elementary.UWP.Dictionaries;
using HtmlAgilityPack;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using VersOne.Epub;
using Windows.Storage;
using Windows.UI.Xaml.Media;

namespace Elementary.ViewModels
{
    public partial class BiblePageViewModel : ObservableObject
    {
        private EpubBook _currentBible;
        private Bible _bible;
        private Book _book;
        private Chapter _chapter;
        private FontFamily _font;
        private Double _fontSize;
        private List<string> _currentBibleBooks;
        private List<int> _currentBookChapters;
        private string _currentChapterText;
        private string _currentChapterContent;
        private ISettings _elementarySettings;

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
            set => SetProperty(ref _currentBibleBooks, value);
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

        public FontFamily Font
        {
            get => _font;
            set => SetProperty(ref _font, value);
        }

        public double FontSize
        {
            get => _fontSize;
            set => SetProperty(ref _fontSize, value);
        }

        public ISettings AppSettings
        {
            get => _elementarySettings;
            set => SetProperty(ref _elementarySettings, value);
        }

        public BiblePageViewModel()
        {}

        public void Initialize()
        {
            var settingsService = App.Services.GetRequiredService<ISettingsService>();
            AppSettings = settingsService.LoadSettings();

            var biblePath = BiblePathDictionary.BibleDictionary[AppSettings.Translation.ToString()];
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
                book.ReadingOrderIndex = 1;
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

            // = Bible.Books.SingleOrDefault(i => i.Title == (string) AppSettings.Book);
            //Chapter = Book.Chapters[int.Parse((string) currentChapter) - 1];

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
    }
}
