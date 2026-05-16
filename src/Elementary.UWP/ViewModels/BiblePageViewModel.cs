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
        private const int ChapterWindowRadius = 2;
        private const int ChapterWindowShiftThreshold = 0;

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
        private Book _chapterIndicesBook;
        private int _displayedChapterStartIndex;
        private int _displayedChapterEndIndex = -1;

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
            _chapterIndicesBook = null;

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

        public async Task<bool> SetCurrentLocationAsync(Book book, int chapterIndex, bool persistSettings = true)
        {
            if (book == null) return false;

            var shouldForceWindowRefresh = !ReferenceEquals(CurrentBook, book) || Chapters.Count == 0;
            await EnsureBookLoadedAsync(book);
            var chapter = ResolveChapter(book, chapterIndex);
            if (chapter == null) return false;

            ApplyCommittedLocation(book, chapter);
            var chapterWindowChanged = UpdateDisplayedChapterWindow(book, chapter, shouldForceWindowRefresh);

            if (persistSettings)
            {
                SaveCurrentLocation();
            }

            return chapterWindowChanged;
        }

        public Task PrepareChapterPickerAsync(Book book)
        {
            if (book == null)
            {
                ChapterIndices = new List<int>();
                _chapterIndicesBook = null;
                return Task.CompletedTask;
            }

            EnsureChapterIndicesForBook(book);
            return Task.CompletedTask;
        }

        public void RestoreChapterPickerToCurrentBook()
        {
            EnsureChapterIndicesForBook(CurrentBook);
        }

        public async Task<bool> LoadInitialChaptersAsync()
        {
            return await EnsureCurrentChapterWindowAsync(forceRefresh: true);
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

        public async Task<bool> UpdateNavigationSettingsAsync(string bookTitle, int chapterIndex, string bookKey = null)
        {
            if (Bible?.Books == null) return false;

            var book = ResolveBook(bookKey, bookTitle);
            if (book == null) return false;

            return await SetCurrentLocationAsync(book, chapterIndex);
        }

        public async Task EnsureBookLoadedAsync(Book book)
        {
            if (book == null || book.IsChaptersLoaded || _bibleService == null || AppSettings == null) return;

            await _bibleService.EnsureBookLoaded(AppSettings.Translation, book);
        }

        public async Task<bool> EnsureCurrentChapterWindowAsync(bool forceRefresh = false)
        {
            if (CurrentBook == null || CurrentChapter == null) return false;

            await EnsureBookLoadedAsync(CurrentBook);
            return UpdateDisplayedChapterWindow(CurrentBook, CurrentChapter, forceRefresh);
        }

        private void ApplyCommittedLocation(Book book, Chapter chapter)
        {
            SetProperty(ref _currentBook, book, nameof(CurrentBook));
            SetProperty(ref _currentChapter, chapter, nameof(CurrentChapter));
            SetProperty(ref _selectedChapterIndex, chapter.Index, nameof(SelectedChapterIndex));
            EnsureChapterIndicesForBook(book);
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
            if (book?.Chapters != null && book.Chapters.Count > 0)
            {
                return book.Chapters.OrderBy(c => c.Index).Select(c => c.Index).ToList();
            }

            return book?.ChapterCount > 0
                ? Enumerable.Range(1, book.ChapterCount).ToList()
                : new List<int>();
        }

        private void EnsureChapterIndicesForBook(Book book)
        {
            if (book == null)
            {
                if (ChapterIndices.Count > 0)
                {
                    ChapterIndices = new List<int>();
                }

                _chapterIndicesBook = null;
                return;
            }

            var expectedChapterCount = book.Chapters?.Count > 0 ? book.Chapters.Count : book.ChapterCount;
            if (ReferenceEquals(_chapterIndicesBook, book) && ChapterIndices.Count == expectedChapterCount)
            {
                return;
            }

            ChapterIndices = CreateChapterIndices(book);
            _chapterIndicesBook = book;
        }

        private void SaveCurrentLocation()
        {
            if (AppSettings == null || CurrentBook == null || CurrentChapter == null || _settingsService == null) return;

            if (!EBookToLocation.EBookTitleToEBook.TryGetValue(CurrentBook.Title, out var bookEnum)) return;

            AppSettings.Book = bookEnum;
            AppSettings.Chapter = CurrentChapter.Index;
            _settingsService.SaveSettings(AppSettings);
        }

        private bool UpdateDisplayedChapterWindow(Book book, Chapter chapter, bool forceRefresh)
        {
            if (book?.Chapters == null || book.Chapters.Count == 0 || chapter == null)
            {
                if (Chapters.Count > 0)
                {
                    Chapters.Clear();
                }

                _displayedChapterStartIndex = 0;
                _displayedChapterEndIndex = -1;
                RestoreChapterPickerToCurrentBook();
                return true;
            }

            var targetChapterIndex = book.Chapters.IndexOf(chapter);
            if (targetChapterIndex < 0)
            {
                targetChapterIndex = 0;
            }

            var (startIndex, endIndex) = GetDesiredDisplayedChapterRange(book.Chapters.Count, targetChapterIndex, forceRefresh);
            if (!forceRefresh
                && startIndex == _displayedChapterStartIndex
                && endIndex == _displayedChapterEndIndex
                && ChaptersMatchWindow(book, startIndex, endIndex))
            {
                return false;
            }

            if (!TrySynchronizeDisplayedChaptersIncrementally(book, startIndex, endIndex))
            {
                Chapters.Clear();
                for (int i = startIndex; i <= endIndex; i++)
                {
                    Chapters.Add(book.Chapters[i]);
                }
            }

            _displayedChapterStartIndex = startIndex;
            _displayedChapterEndIndex = endIndex;
            RestoreChapterPickerToCurrentBook();
            return true;
        }

        private (int startIndex, int endIndex) GetDesiredDisplayedChapterRange(int totalChapterCount, int targetChapterIndex, bool forceRefresh)
        {
            if (totalChapterCount <= 0)
            {
                return (0, -1);
            }

            var maximumWindowSize = (ChapterWindowRadius * 2) + 1;
            if (forceRefresh || _displayedChapterEndIndex < _displayedChapterStartIndex)
            {
                return CreateCenteredDisplayedRange(totalChapterCount, targetChapterIndex, maximumWindowSize);
            }

            if (targetChapterIndex < _displayedChapterStartIndex || targetChapterIndex > _displayedChapterEndIndex)
            {
                return CreateCenteredDisplayedRange(totalChapterCount, targetChapterIndex, maximumWindowSize);
            }

            var shouldShiftBackward = targetChapterIndex <= _displayedChapterStartIndex + ChapterWindowShiftThreshold;
            var shouldShiftForward = targetChapterIndex >= _displayedChapterEndIndex - ChapterWindowShiftThreshold;
            if (!shouldShiftBackward && !shouldShiftForward)
            {
                return (_displayedChapterStartIndex, _displayedChapterEndIndex);
            }

            return CreateCenteredDisplayedRange(totalChapterCount, targetChapterIndex, maximumWindowSize);
        }

        private static (int startIndex, int endIndex) CreateCenteredDisplayedRange(int totalChapterCount, int targetChapterIndex, int maximumWindowSize)
        {
            if (totalChapterCount <= maximumWindowSize)
            {
                return (0, totalChapterCount - 1);
            }

            var startIndex = Math.Max(0, targetChapterIndex - ChapterWindowRadius);
            var endIndex = Math.Min(totalChapterCount - 1, startIndex + maximumWindowSize - 1);
            startIndex = Math.Max(0, endIndex - maximumWindowSize + 1);
            return (startIndex, endIndex);
        }

        private bool ChaptersMatchWindow(Book book, int startIndex, int endIndex)
        {
            var expectedCount = endIndex - startIndex + 1;
            if (expectedCount != Chapters.Count)
            {
                return false;
            }

            for (int i = 0; i < expectedCount; i++)
            {
                if (!ReferenceEquals(Chapters[i], book.Chapters[startIndex + i]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool TrySynchronizeDisplayedChaptersIncrementally(Book book, int startIndex, int endIndex)
        {
            if (Chapters.Count == 0 || !DisplayedChaptersBelongToBook(book))
            {
                return false;
            }

            while (_displayedChapterStartIndex < startIndex && Chapters.Count > 0)
            {
                Chapters.RemoveAt(0);
                _displayedChapterStartIndex++;
            }

            while (_displayedChapterStartIndex > startIndex)
            {
                var insertIndex = _displayedChapterStartIndex - 1;
                Chapters.Insert(0, book.Chapters[insertIndex]);
                _displayedChapterStartIndex--;
            }

            while (_displayedChapterEndIndex > endIndex && Chapters.Count > 0)
            {
                Chapters.RemoveAt(Chapters.Count - 1);
                _displayedChapterEndIndex--;
            }

            while (_displayedChapterEndIndex < endIndex)
            {
                var appendIndex = _displayedChapterEndIndex + 1;
                Chapters.Add(book.Chapters[appendIndex]);
                _displayedChapterEndIndex++;
            }

            return ChaptersMatchWindow(book, startIndex, endIndex);
        }

        private bool DisplayedChaptersBelongToBook(Book book)
        {
            if (book?.Chapters == null)
            {
                return false;
            }

            for (int i = 0; i < Chapters.Count; i++)
            {
                if (!book.Chapters.Contains(Chapters[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
