using CommunityToolkit.Mvvm.ComponentModel;
using Elementary.Core.Dictionaries;
using Elementary.Core.Enums;
using Elementary.Core.Extensions;
using Elementary.Core.Interfaces;
using Elementary.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private ObservableCollection<Chapter> _chapters;
        private bool _isLoadingMore;

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
                    EnsureCurrentBookLoaded();
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
            set
            {
                if (SetProperty(ref _appSettings, value))
                {
                    OnPropertyChanged(nameof(Font));
                    OnPropertyChanged(nameof(FontSize));
                }
            }
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

        public ObservableCollection<Chapter> Chapters
        {
            get => _chapters;
            set => SetProperty(ref _chapters, value);
        }

        public bool IsLoadingMore
        {
            get => _isLoadingMore;
            set => SetProperty(ref _isLoadingMore, value);
        }

        public BiblePageViewModel()
        {
            Chapters = new ObservableCollection<Chapter>();
        }

        public async Task Initialize()
        {
            _settingsService = App.Services.GetRequiredService<ISettingsService>();
            AppSettings = _settingsService.GetSettings();

            _bibleService = App.Services.GetRequiredService<IBibleService>();
            Bible = await _bibleService.GetBible(ETranslation.NET);

            CurrentBook = Bible.Books.FirstOrDefault(b =>
                EBookToLocation.EBookTitleToEBook.TryGetValue(b.Title, out var bookEnum) && bookEnum == AppSettings.Book)
                ?? Bible.Books.FirstOrDefault();
            CurrentChapter = CurrentBook?.Chapters.FirstOrDefault(c => c.Index == AppSettings.Chapter) ?? CurrentBook?.Chapters.FirstOrDefault() ?? new Chapter();
            // Ensure the selected chapter index reflects the current chapter so the ComboBox shows correctly
            SelectedChapterIndex = CurrentChapter?.Index ?? 1;

            // Initialize with current chapter and load adjacent chapters
            LoadInitialChapters();

            IsLoaded = true;
        }

        public void LoadInitialChapters()
        {
            Chapters.Clear();
            if (CurrentChapter == null) return;

            // Add current chapter first
            Chapters.Add(CurrentChapter);
            
            // Load previous chapter
            var currentBook = Bible.Books.FirstOrDefault(b => b.Chapters.Contains(CurrentChapter));
            if (currentBook != null)
            {
                var prevChapter = currentBook.Chapters.FirstOrDefault(c => c.Index == CurrentChapter.Index - 1);
                if (prevChapter != null)
                {
                    Chapters.Insert(0, prevChapter);
                }
                else
                {
                    // Try previous book's last chapter
                    var prevBook = Bible.Books.FirstOrDefault(b => b.ReadingOrderIndex == currentBook.ReadingOrderIndex - 1);
                    EnsureBookLoaded(prevBook);
                    if (prevBook?.Chapters.Count > 0)
                    {
                        Chapters.Insert(0, prevBook.Chapters.Last());
                    }
                }
            }
            
            // Load next chapter
            LoadNextChapter();
        }

        public void LoadNextChapter()
        {
            if (IsLoadingMore) return;

            IsLoadingMore = true;

            try
            {
                var lastChapter = Chapters.LastOrDefault();
                if (lastChapter == null)
                {
                    return;
                }

                // Find the next chapter
                var currentBook = Bible.Books.FirstOrDefault(b => b.Chapters.Contains(lastChapter));
                if (currentBook != null)
                {
                    var nextChapter = currentBook.Chapters.FirstOrDefault(c => c.Index == lastChapter.Index + 1);
                    if (nextChapter != null)
                    {
                        Chapters.Add(nextChapter);
                    }
                    else
                    {
                        // Move to next book
                        var nextBook = Bible.Books.FirstOrDefault(b => b.ReadingOrderIndex == currentBook.ReadingOrderIndex + 1);
                        EnsureBookLoaded(nextBook);
                        if (nextBook?.Chapters.Count > 0)
                        {
                            Chapters.Add(nextBook.Chapters.First());
                        }
                    }
                }
            }
            finally
            {
                IsLoadingMore = false;
            }
        }

        public void LoadPreviousChapter()
        {
            if (IsLoadingMore) return;

            IsLoadingMore = true;

            try
            {
                var firstChapter = Chapters.FirstOrDefault();
                if (firstChapter == null)
                {
                    return;
                }

                // Find the previous chapter
                var currentBook = Bible.Books.FirstOrDefault(b => b.Chapters.Contains(firstChapter));
                if (currentBook != null)
                {
                    var prevChapter = currentBook.Chapters.FirstOrDefault(c => c.Index == firstChapter.Index - 1);
                    if (prevChapter != null)
                    {
                        Chapters.Insert(0, prevChapter);
                    }
                    else
                    {
                        // Move to previous book
                        var prevBook = Bible.Books.FirstOrDefault(b => b.ReadingOrderIndex == currentBook.ReadingOrderIndex - 1);
                        EnsureBookLoaded(prevBook);
                        if (prevBook?.Chapters.Count > 0)
                        {
                            Chapters.Insert(0, prevBook.Chapters.Last());
                        }
                    }
                }
            }
            finally
            {
                IsLoadingMore = false;
            }
        }

        public void UpdateCurrentChapterFromScroll(Chapter chapter)
        {
            if (chapter == null) return;

            // Find which book this chapter belongs to
            var book = Bible.Books.FirstOrDefault(b => b.Chapters.Contains(chapter));
            if (book != null && book != CurrentBook)
            {
                // Book changed - update without triggering reload
                SetProperty(ref _currentBook, book, nameof(CurrentBook));
                OnPropertyChanged(nameof(ChapterIndices));
            }

            // Update the current chapter and selected index
            SetProperty(ref _currentChapter, chapter, nameof(CurrentChapter));
            SetProperty(ref _selectedChapterIndex, chapter.Index, nameof(SelectedChapterIndex));

            // Update settings
            if (IsLoaded)
            {
                UpdateBookSetting();
                UpdateChapterSetting();
            }
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
                    EnsureBookLoaded(book);
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

        private void EnsureCurrentBookLoaded()
        {
            EnsureBookLoaded(_currentBook);
        }

        private void EnsureBookLoaded(Book book)
        {
            if (book == null || book.IsChaptersLoaded || _bibleService == null || AppSettings == null) return;

            Task.Run(async () => await _bibleService.EnsureBookLoaded(AppSettings.Translation, book)).GetAwaiter().GetResult();
            OnPropertyChanged(nameof(ChapterIndices));
        }
    }
}
