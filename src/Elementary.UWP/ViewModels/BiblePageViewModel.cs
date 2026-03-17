using CommunityToolkit.Mvvm.ComponentModel;
using Elementary.Core.Dictionaries;
using Elementary.Core.Enums;
using Elementary.Core.Extensions;
using Elementary.Core.Interfaces;
using Elementary.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
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
                    EnsureBookLoaded(_currentBook);
                    OnPropertyChanged(nameof(ChapterIndices));
                    // Automatically update chapter when book changes
                    CurrentChapter = _currentBook?.Chapters.FirstOrDefault();
                    _selectedChapterIndex = CurrentChapter?.Index ?? 1;
                    OnPropertyChanged(nameof(SelectedChapterIndex));

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

        public void RefreshSettings()
        {
            if (_settingsService == null) return;
            AppSettings = _settingsService.GetSettings();
        }

        public async Task Initialize()
        {
            _settingsService = App.Services.GetRequiredService<ISettingsService>();
            AppSettings = _settingsService.GetSettings();

            _bibleService = App.Services.GetRequiredService<IBibleService>();
            Bible = await _bibleService.GetBible(AppSettings.Translation);

            CurrentBook = Bible.Books.FirstOrDefault(b =>
                EBookToLocation.EBookTitleToEBook.TryGetValue(b.Title, out var bookEnum) && bookEnum == AppSettings.Book)
                ?? Bible.Books.FirstOrDefault();
            CurrentChapter = CurrentBook?.Chapters.FirstOrDefault(c => c.Index == AppSettings.Chapter) ?? CurrentBook?.Chapters.FirstOrDefault() ?? new Chapter();
            // Ensure the selected chapter index reflects the current chapter so the ComboBox shows correctly
            SelectedChapterIndex = CurrentChapter?.Index ?? 1;

            // Initialize with current chapter and load adjacent chapters
            await LoadInitialChaptersAsync();

            IsLoaded = true;
        }

        public async Task LoadInitialChaptersAsync()
        {
            Chapters.Clear();
            if (CurrentBook == null) return;

            await EnsureBookLoadedAsync(CurrentBook);
            if (CurrentBook.Chapters == null || CurrentBook.Chapters.Count == 0) return;

            foreach (var chapter in CurrentBook.Chapters.OrderBy(c => c.Index))
            {
                Chapters.Add(chapter);
            }
        }

        public Task LoadNextChapterAsync()
        {
            return Task.CompletedTask;
        }

        public Task LoadPreviousChapterAsync()
        {
            return Task.CompletedTask;
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
                if (EBookToLocation.EBookTitleToEBook.TryGetValue(CurrentBook.Title, out var bookEnum))
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

        public async Task UpdateNavigationSettingsAsync(string bookTitle, int chapterIndex, string bookKey = null)
        {
            if (Bible?.Books == null) return;

            Book book = null;
            if (!string.IsNullOrWhiteSpace(bookKey) && Enum.TryParse(bookKey, out EBook requestedBook))
            {
                book = Bible.Books.FirstOrDefault(b =>
                    EBookToLocation.EBookTitleToEBook.TryGetValue(b.Title, out var mappedBook) && mappedBook == requestedBook);
            }

            if (book == null && !string.IsNullOrWhiteSpace(bookTitle))
            {
                book = Bible.Books.FirstOrDefault(b => string.Equals(b.Title, bookTitle, StringComparison.OrdinalIgnoreCase));
            }

            if (book != null)
            {
                await EnsureBookLoadedAsync(book);
                CurrentBook = book;
                var chapter = book.Chapters.FirstOrDefault(c => c.Index == chapterIndex);
                if (chapter != null)
                {
                    CurrentChapter = chapter;
                    SelectedChapterIndex = chapterIndex;
                }
            }
        }

        private async Task EnsureBookLoadedAsync(Book book)
        {
            if (book == null || book.IsChaptersLoaded || _bibleService == null || AppSettings == null) return;

            await _bibleService.EnsureBookLoaded(AppSettings.Translation, book);
            OnPropertyChanged(nameof(ChapterIndices));
        }

        // Sync wrapper for use in property setters where async is not possible
        private void EnsureBookLoaded(Book book)
        {
            if (book == null || book.IsChaptersLoaded || _bibleService == null || AppSettings == null) return;

            // Task.Run avoids SynchronizationContext deadlock; ConfigureAwait(false) for defense-in-depth
            Task.Run(async () => await _bibleService.EnsureBookLoaded(AppSettings.Translation, book).ConfigureAwait(false)).GetAwaiter().GetResult();
            OnPropertyChanged(nameof(ChapterIndices));
        }
    }
}
