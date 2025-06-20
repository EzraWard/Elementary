using CommunityToolkit.Mvvm.ComponentModel;
using Elementary.Core.Dictionaries;
using Elementary.Core.Enums;
using Elementary.Core.Extensions;
using Elementary.Core.Interfaces;
using Elementary.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Elementary.ViewModels
{
    public partial class BiblePageViewModel : ObservableObject
    {
        private Bible _bible;
        private Book _currentBook;
        private Chapter _currentChapter;
        private int _selectedChapterIndex;
        private ISettings _appSettings;
        private ISettingsService _settingsService;
        private IBibleService _bibleService;
        private bool _isLoaded;

        public Bible Bible
        {
            get => _bible;
            set => SetProperty(ref _bible, value);
        }

        public Book CurrentBook
        {
            get => _currentBook;
            set
            {
                if (SetProperty(ref _currentBook, value))
                {
                    OnPropertyChanged(nameof(ChapterIndices));
                    // Automatically update chapter when book changes
                    CurrentChapter = _currentBook?.Chapters.FirstOrDefault();
                    SelectedChapterIndex = 1;

                    // Update app settings when book changes
                    UpdateBookSetting();
                }
            }
        }

        public Chapter CurrentChapter
        {
            get => _currentChapter;
            set
            {
                if (SetProperty(ref _currentChapter, value))
                {
                    OnPropertyChanged(nameof(SelectedChapterIndex));

                    // Update app settings when chapter changes
                    UpdateChapterSetting();
                }
            }
        }

        public List<int> ChapterIndices =>
            CurrentBook?.Chapters != null
                ? Enumerable.Range(1, _currentBook.Chapters.Count).ToList()
                : new List<int>();

        public int SelectedChapterIndex
        {
            get => _selectedChapterIndex;
            set
            {
                if (SetProperty(ref _selectedChapterIndex, value))
                {
                    // Update CurrentChapter when SelectedChapterIndex changes
                    if (CurrentBook?.Chapters != null)
                    {
                        // Find chapter by index (assuming 1-based indexing)
                        CurrentChapter = CurrentBook.Chapters.FirstOrDefault(c => c.Index == value)
                                      ?? CurrentBook.Chapters.ElementAtOrDefault(value - 1);
                    }
                }
            }
        }

        public ISettings AppSettings
        {
            get => _appSettings;
            set => SetProperty(ref _appSettings, value);
        }

        public int FontSize
        {
            get
            {
                return AppSettings != null ? FontSizeConverter.EFontSizeToSize[AppSettings.FontSize] : 16;
            }
        }

        public string Font => AppSettings?.Font.GetDisplayName();

        public bool IsLoaded
        {
            get => _isLoaded;
            set => SetProperty(ref _isLoaded, value);
        }

        public BiblePageViewModel()
        { }

        public async Task Initialize()
        {
            _settingsService = App.Services.GetRequiredService<ISettingsService>();
            AppSettings = _settingsService.GetSettings();

            _bibleService = App.Services.GetRequiredService<IBibleService>();
            Bible = await _bibleService.GetBible(ETranslation.NET);

            CurrentBook = Bible.Books.FirstOrDefault(b => b.Title == AppSettings.Book.ToString()) ?? Bible.Books.FirstOrDefault();
            CurrentChapter = CurrentBook?.Chapters.FirstOrDefault(c => c.Index == AppSettings.Chapter) ?? CurrentBook?.Chapters.FirstOrDefault() ?? new Chapter();
            SelectedChapterIndex = CurrentChapter.Index;

            IsLoaded = true;
        }

        private void UpdateBookSetting()
        {
            if (AppSettings != null && CurrentBook != null && _settingsService != null && IsLoaded)
            {
                // Parse the book title to the appropriate enum value
                if (System.Enum.TryParse<EBook>(CurrentBook.Title, out var bookEnum))
                {
                    AppSettings.Book = bookEnum;
                    _settingsService.SaveSettings(AppSettings);
                }
            }
        }

        private void UpdateChapterSetting()
        {
            if (AppSettings != null && CurrentChapter != null && _settingsService != null && IsLoaded)
            {
                AppSettings.Chapter = CurrentChapter.Index;
                _settingsService.SaveSettings(AppSettings);
            }
        }

        // Optional: Public method to manually update settings (useful for navigation scenarios)
        public void UpdateNavigationSettings(string bookTitle, int chapterIndex)
        {
            if (Bible?.Books != null)
            {
                var book = Bible.Books.FirstOrDefault(b => b.Title == bookTitle);
                if (book != null)
                {
                    CurrentBook = book;
                    var chapter = book.Chapters.FirstOrDefault(c => c.Index == chapterIndex);
                    if (chapter != null)
                    {
                        CurrentChapter = chapter;
                        SelectedChapterIndex = chapterIndex;
                    }
                }
            }
        }
    }
}