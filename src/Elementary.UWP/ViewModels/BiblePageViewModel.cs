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
        private const int InitialFollowingBookCount = 1;

        private Bible _bible;
        private Book _currentBook;
        private Chapter _currentChapter;
        private int _selectedChapterIndex = 1;
        private List<int> _chapterIndices = new List<int>();
        private ISettings _appSettings;
        private ISettingsService _settingsService;
        private IBibleService _bibleService;
        private bool _isLoaded;
        private ObservableCollection<BibleReaderItem> _readerItems;
        private Book _chapterIndicesBook;

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

        public ObservableCollection<BibleReaderItem> ReaderItems
        {
            get => _readerItems;
            set => SetProperty(ref _readerItems, value);
        }

        public BiblePageViewModel()
        {
            ReaderItems = new ObservableCollection<BibleReaderItem>();
        }

        public async Task Initialize()
        {
            _settingsService = App.Services.GetRequiredService<ISettingsService>();
            _bibleService = App.Services.GetRequiredService<IBibleService>();

            AppSettings = _settingsService.GetSettings();
            Bible = await _bibleService.GetBible(AppSettings.Translation);

            IsLoaded = false;
            ReaderItems.Clear();
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

        public bool RefreshSettingsAndDetectTranslationChange()
        {
            _settingsService = _settingsService ?? App.Services.GetRequiredService<ISettingsService>();

            var updatedSettings = _settingsService.GetSettings();
            var translationChanged = AppSettings != null
                                     && AppSettings.Translation != updatedSettings.Translation;
            AppSettings = updatedSettings;
            return translationChanged;
        }

        public async Task<bool> SetCurrentLocationAsync(Book book, int chapterIndex, bool persistSettings = true)
        {
            if (book == null) return false;

            var streamChanged = !IsBookInReaderStream(book);
            await EnsureBookLoadedAsync(book);
            var chapter = ResolveChapter(book, chapterIndex);
            if (chapter == null) return false;

            ApplyCommittedLocation(book, chapter);
            if (streamChanged)
            {
                await ResetReaderStreamAroundBookAsync(book);
            }

            if (persistSettings)
            {
                SaveCurrentLocation();
            }

            return streamChanged;
        }

        public async Task PrepareChapterPickerAsync(Book book)
        {
            if (book == null)
            {
                ChapterIndices = new List<int>();
                _chapterIndicesBook = null;
                return;
            }

            await EnsureBookLoadedAsync(book);
            EnsureChapterIndicesForBook(book);
        }

        public void RestoreChapterPickerToCurrentBook()
        {
            EnsureChapterIndicesForBook(CurrentBook);
        }

        public async Task<bool> LoadInitialChaptersAsync()
        {
            if (CurrentBook == null)
            {
                return false;
            }

            await EnsureBookLoadedAsync(CurrentBook);
            var streamChanged = !IsBookInReaderStream(CurrentBook);
            if (streamChanged)
            {
                await ResetReaderStreamAroundBookAsync(CurrentBook);
            }

            return streamChanged;
        }

        public void UpdateCurrentChapterFromScroll(Chapter chapter)
        {
            if (chapter == null || Bible?.Books == null) return;

            var book = Bible.Books.FirstOrDefault(b => b.Chapters != null && b.Chapters.Contains(chapter));
            if (book == null) return;

            ApplyCommittedLocation(book, chapter);
        }

        public void UpdateCurrentChapterFromScroll(BibleReaderItem readerItem)
        {
            if (readerItem?.Book == null || readerItem.Chapter == null) return;

            ApplyCommittedLocation(readerItem.Book, readerItem.Chapter);
        }

        public void PersistCurrentLocation()
        {
            SaveCurrentLocation();
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

        public BibleReaderItem GetReaderItemForCurrentChapter()
        {
            return GetReaderItem(CurrentBook, CurrentChapter);
        }

        public BibleReaderItem GetReaderHeaderForCurrentBook()
        {
            return GetReaderHeader(CurrentBook);
        }

        public BibleReaderItem GetReaderHeader(Book book)
        {
            if (book == null) return null;

            return ReaderItems.FirstOrDefault(item =>
                item.IsBookHeader
                && ReferenceEquals(item.Book, book));
        }

        public BibleReaderItem GetReaderItem(Book book, Chapter chapter)
        {
            if (book == null || chapter == null) return null;

            return ReaderItems.FirstOrDefault(item =>
                item.IsChapter
                && ReferenceEquals(item.Book, book)
                && ReferenceEquals(item.Chapter, chapter));
        }

        public BibleReaderItem GetReaderItem(string bookTitle, int chapterIndex, string bookKey = null)
        {
            var book = ResolveBook(bookKey, bookTitle);
            if (book == null) return null;

            var chapter = ResolveChapter(book, chapterIndex);
            return GetReaderItem(book, chapter);
        }

        public async Task<bool> AppendNextBookAsync()
        {
            var lastBook = GetLastBookInReaderStream();
            var nextBook = GetAdjacentBook(lastBook, 1);
            if (nextBook == null || IsBookInReaderStream(nextBook))
            {
                return false;
            }

            await EnsureBookLoadedAsync(nextBook);
            AppendBookToReaderStream(nextBook);
            return true;
        }

        public async Task<bool> PrependPreviousBookAsync()
        {
            var firstBook = GetFirstBookInReaderStream();
            var previousBook = GetAdjacentBook(firstBook, -1);
            if (previousBook == null || IsBookInReaderStream(previousBook))
            {
                return false;
            }

            await EnsureBookLoadedAsync(previousBook);
            PrependBookToReaderStream(previousBook);
            return true;
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

            // Merge the location into the latest stored settings. The reader page can be cached,
            // so saving its older settings object must not revert a change made in Settings.
            var latestSettings = _settingsService.GetSettings();
            latestSettings.Book = bookEnum;
            latestSettings.Chapter = CurrentChapter.Index;
            _settingsService.SaveSettings(latestSettings);

            AppSettings.Book = bookEnum;
            AppSettings.Chapter = CurrentChapter.Index;
        }

        private async Task ResetReaderStreamAroundBookAsync(Book book)
        {
            ReaderItems.Clear();
            if (book == null)
            {
                RestoreChapterPickerToCurrentBook();
                return;
            }

            await EnsureBookLoadedAsync(book);
            AppendBookToReaderStream(book);

            var followingBook = book;
            for (int i = 0; i < InitialFollowingBookCount; i++)
            {
                followingBook = GetAdjacentBook(followingBook, 1);
                if (followingBook == null)
                {
                    break;
                }

                await EnsureBookLoadedAsync(followingBook);
                AppendBookToReaderStream(followingBook);
            }

            RestoreChapterPickerToCurrentBook();
        }

        private bool IsBookInReaderStream(Book book)
        {
            return book != null && ReaderItems.Any(item => ReferenceEquals(item.Book, book));
        }

        private Book GetFirstBookInReaderStream()
        {
            return ReaderItems.FirstOrDefault(item => item.Book != null)?.Book;
        }

        private Book GetLastBookInReaderStream()
        {
            return ReaderItems.LastOrDefault(item => item.Book != null)?.Book;
        }

        private Book GetAdjacentBook(Book book, int offset)
        {
            if (book == null || Bible?.Books == null) return null;

            var bookIndex = Bible.Books.IndexOf(book);
            if (bookIndex < 0) return null;

            var adjacentIndex = bookIndex + offset;
            return adjacentIndex >= 0 && adjacentIndex < Bible.Books.Count
                ? Bible.Books[adjacentIndex]
                : null;
        }

        private void AppendBookToReaderStream(Book book)
        {
            if (book == null || IsBookInReaderStream(book)) return;

            RemoveBottomSpacer();
            foreach (var item in CreateReaderItems(book))
            {
                ReaderItems.Add(item);
            }

            AddBottomSpacer();
        }

        private void PrependBookToReaderStream(Book book)
        {
            if (book == null || IsBookInReaderStream(book)) return;

            var items = CreateReaderItems(book).ToList();
            for (int i = items.Count - 1; i >= 0; i--)
            {
                ReaderItems.Insert(0, items[i]);
            }
        }

        private void AddBottomSpacer()
        {
            if (!ReaderItems.Any(item => item.IsBottomSpacer))
            {
                ReaderItems.Add(BibleReaderItem.CreateBottomSpacer());
            }
        }

        private void RemoveBottomSpacer()
        {
            var spacer = ReaderItems.FirstOrDefault(item => item.IsBottomSpacer);
            if (spacer != null)
            {
                ReaderItems.Remove(spacer);
            }
        }

        private static IEnumerable<BibleReaderItem> CreateReaderItems(Book book)
        {
            yield return BibleReaderItem.CreateBookHeader(book);

            if (book?.Chapters == null)
            {
                yield break;
            }

            foreach (var chapter in book.Chapters)
            {
                yield return BibleReaderItem.CreateChapter(book, chapter);
            }
        }
    }

    public class BibleReaderItem
    {
        public Book Book { get; private set; }
        public Chapter Chapter { get; private set; }
        public bool IsBookHeader { get; private set; }
        public bool IsBottomSpacer { get; private set; }
        public bool IsChapter => Chapter != null;
        public string BookTitle => Book?.Title ?? string.Empty;
        public int ChapterIndex => Chapter?.Index ?? 0;
        public double SpacerHeight => IsBottomSpacer ? 520d : 0d;
        public ObservableCollection<ChapterDisplayLine> DisplayLines => Chapter?.DisplayLines;

        public static BibleReaderItem CreateBookHeader(Book book)
        {
            return new BibleReaderItem
            {
                Book = book,
                IsBookHeader = true
            };
        }

        public static BibleReaderItem CreateChapter(Book book, Chapter chapter)
        {
            return new BibleReaderItem
            {
                Book = book,
                Chapter = chapter
            };
        }

        public static BibleReaderItem CreateBottomSpacer()
        {
            return new BibleReaderItem
            {
                IsBottomSpacer = true
            };
        }
    }
}
