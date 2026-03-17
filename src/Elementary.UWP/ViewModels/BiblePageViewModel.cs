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
        private int _selectedChapterIndex = 1;
        private List<int> _chapterIndices = new List<int>();
        private ISettings _appSettings;
        private ISettingsService _settingsService;
        private IBibleService _bibleService;
        private bool _isLoaded;
        private ObservableCollection<Chapter> _chapters;
        private bool _isLoadingMore;

        public Bible Bible
        {
            get => _bible;
            private set => SetProperty(ref _bible, value);
        }

        public Book CurrentBook
        {
            get => _currentBook;
            private set => SetProperty(ref _currentBook, value);
        }

        public Chapter CurrentChapter
        {
            get => _currentChapter;
            private set => SetProperty(ref _currentChapter, value);
        }

        public int SelectedChapterIndex
        {
            get => _selectedChapterIndex;
            private set => SetProperty(ref _selectedChapterIndex, value);
        }

        public List<int> ChapterIndices
        {
            get => _chapterIndices;
            private set => SetProperty(ref _chapterIndices, value);
        }

        public ISettings AppSettings
        {
            get => _appSettings;
            private set
            {
                if (SetProperty(ref _appSettings, value))
                {
                    OnPropertyChanged(nameof(Font));
                    OnPropertyChanged(nameof(FontSize));
                }
            }
        }

        public int FontSize => AppSettings != null ? FontSizeConverter.EFontSizeToSize[AppSettings.FontSize] : 16;

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
            _bibleService = App.Services.GetRequiredService<IBibleService>();

            AppSettings = _settingsService.GetSettings();
            Bible = await _bibleService.GetBible(AppSettings.Translation);

            IsLoaded = false;
            Chapters.Clear();
            ChapterIndices = new List<int>();

            var initialBook = ResolveBook(AppSettings.Book) ?? Bible?.Books?.FirstOrDefault();
            if (initialBook == null)
            {
                CurrentBook = null;
                CurrentChapter = null;
                SelectedChapterIndex = 1;
                return;
            }

            await SetCurrentLocationAsync(initialBook, AppSettings.Chapter, persistSettings: false);
        }

        public async Task SetCurrentLocationAsync(Book book, int chapterIndex, bool persistSettings = true)
        {
            if (book == null) return;

            await EnsureBookLoadedAsync(book);
            var chapter = ResolveChapter(book, chapterIndex);
            if (chapter == null) return;

            ApplyCommittedLocation(book, chapter);
            await LoadCurrentBookChaptersAsync();

            if (persistSettings)
            {
                SaveCurrentLocation();
            }
        }

        public async Task PrepareChapterPickerAsync(Book book)
        {
            if (book == null)
            {
                ChapterIndices = new List<int>();
                return;
            }

            await EnsureBookLoadedAsync(book);
            ChapterIndices = CreateChapterIndices(book);
        }

        public void RestoreChapterPickerToCurrentBook()
        {
            ChapterIndices = CreateChapterIndices(CurrentBook);
        }

        public async Task LoadInitialChaptersAsync()
        {
            await LoadCurrentBookChaptersAsync();
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
            if (chapter == null || Bible?.Books == null) return;

            var book = Bible.Books.FirstOrDefault(b => b.Chapters != null && b.Chapters.Contains(chapter));
            if (book == null) return;

            ApplyCommittedLocation(book, chapter);

            if (IsLoaded)
            {
                SaveCurrentLocation();
            }
        }

        public async Task UpdateNavigationSettingsAsync(string bookTitle, int chapterIndex, string bookKey = null)
        {
            if (Bible?.Books == null) return;

            var book = ResolveBook(bookKey, bookTitle);
            if (book == null) return;

            await SetCurrentLocationAsync(book, chapterIndex);
        }

        public async Task EnsureBookLoadedAsync(Book book)
        {
            if (book == null || book.IsChaptersLoaded || _bibleService == null || AppSettings == null) return;

            await _bibleService.EnsureBookLoaded(AppSettings.Translation, book);
        }

        private async Task LoadCurrentBookChaptersAsync()
        {
            Chapters.Clear();
            if (CurrentBook == null) return;

            await EnsureBookLoadedAsync(CurrentBook);
            if (CurrentBook.Chapters == null || CurrentBook.Chapters.Count == 0) return;

            foreach (var chapter in CurrentBook.Chapters.OrderBy(c => c.Index))
            {
                Chapters.Add(chapter);
            }

            RestoreChapterPickerToCurrentBook();
        }

        private void ApplyCommittedLocation(Book book, Chapter chapter)
        {
            SetProperty(ref _currentBook, book, nameof(CurrentBook));
            SetProperty(ref _currentChapter, chapter, nameof(CurrentChapter));
            SetProperty(ref _selectedChapterIndex, chapter.Index, nameof(SelectedChapterIndex));
            RestoreChapterPickerToCurrentBook();
        }

        private Book ResolveBook(EBook bookEnum)
        {
            if (Bible?.Books == null) return null;

            return Bible.Books.FirstOrDefault(b =>
                       EBookToLocation.EBookTitleToEBook.TryGetValue(b.Title, out var mappedBook) && mappedBook == bookEnum)
                   ?? Bible.Books.FirstOrDefault();
        }

        private Book ResolveBook(string bookKey, string bookTitle)
        {
            if (Bible?.Books == null) return null;

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

            return book;
        }

        private static Chapter ResolveChapter(Book book, int chapterIndex)
        {
            if (book?.Chapters == null || book.Chapters.Count == 0) return null;

            var normalizedChapterIndex = chapterIndex > 0 ? chapterIndex : 1;
            return book.Chapters.FirstOrDefault(c => c.Index == normalizedChapterIndex)
                   ?? book.Chapters.ElementAtOrDefault(normalizedChapterIndex - 1)
                   ?? book.Chapters.FirstOrDefault();
        }

        private static List<int> CreateChapterIndices(Book book)
        {
            return book?.Chapters != null
                ? book.Chapters.OrderBy(c => c.Index).Select(c => c.Index).ToList()
                : new List<int>();
        }

        private void SaveCurrentLocation()
        {
            if (AppSettings == null || CurrentBook == null || CurrentChapter == null || _settingsService == null) return;

            if (!EBookToLocation.EBookTitleToEBook.TryGetValue(CurrentBook.Title, out var bookEnum)) return;

            AppSettings.Book = bookEnum;
            AppSettings.Chapter = CurrentChapter.Index;
            _settingsService.SaveSettings(AppSettings);
        }
    }
}
