using Elementary.Core.Models;
using Elementary.Core.Parsers;
using Microsoft.UI.Xaml.Input;
using System.Collections.ObjectModel;
using System.Reflection;

namespace Elementary.Uno;

public sealed partial class MainPage : Page
{
    private readonly Services.EmbeddedBibleCatalog _catalog = new(Assembly.GetExecutingAssembly());
    private Services.ScriptureBook? _book;
    private bool _changingBook;
    private int _loadVersion;
    private int _searchVersion;
    private readonly ObservableCollection<ReaderLine> _readerLines = new();
    private UsfmBook? _loadedBook;

    public MainPage()
    {
        InitializeComponent();
        InitializeShell();
        VerseList.ItemsSource = _readerLines;
        Loaded += MainPage_Loaded;
        KeyDown += MainPage_KeyDown;
    }

    private void MainPage_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape && SearchPanel.Visibility == Visibility.Visible)
        {
            e.Handled = true;
            CloseSearchPanelButton_Click(sender, e);
        }
    }

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= MainPage_Loaded;

        BibleBookComboBox.ItemsSource = _catalog.Books;
        var book = _catalog.Books.FirstOrDefault(item => item.Id == _settings.Book) ?? _catalog.Books[0];
        await NavigateToBookAsync(book, _settings.Chapter);
    }

    private void BibleBookComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_changingBook || BibleBookComboBox.SelectedItem is not Services.ScriptureBook book) return;
        if (book == _book) { _pendingBook = null; SynchronizePickers(); return; }
        _pendingBook = book;
        BookChapterComboBox.IsEnabled = true;
        _changingBook = true;
        BookChapterComboBox.ItemsSource = Enumerable.Range(1, Elementary.Core.Dictionaries.EBookToLocation.EBookToChapterCount[book.Id]).ToList();
        BookChapterComboBox.SelectedItem = null;
        _changingBook = false;
    }

    private void BibleBookComboBox_DropDownClosed(object sender, object args)
    {
        // Open after the book popup closes; otherwise Uno closes both popups together.
        DispatcherQueue.TryEnqueue(() => { if (_pendingBook is not null) BookChapterComboBox.IsDropDownOpen = true; });
    }
    private async Task NavigateToBookAsync(Services.ScriptureBook book, int chapter)
    {
        _pendingBook = null;
        var version = ++_loadVersion;
        ++_highlightVersion;
        _highlightedLine?.Highlight(false);
        _highlightedLine = null;
        _positioningReader = true;
        BookChapterComboBox.IsEnabled = false;
        NavigationError.IsOpen = false;
        _ = ShowNavigationLoadingAsync(version);
        try
        {
            var loaded = await _catalog.LoadAsync(book);
            if (version != _loadVersion) return;
            _changingBook = true;
            _book = book;
            _loadedBook = loaded;
            BibleBookComboBox.SelectedItem = book;
            BookHeading.Text = book.Title;
            BookChapterComboBox.ItemsSource = loaded.Chapters.Select(item => item.Index).ToList();
            BookChapterComboBox.SelectedItem = Math.Clamp(chapter, 1, loaded.Chapters.Count);
            _changingBook = false;
            RecordLocation((int)BookChapterComboBox.SelectedItem);
            // A collapsed reader has no usable row geometry. Position it when shown.
            if (_currentPage != "Bible") { _readerNeedsPosition = true; return; }
            await ResetReaderWindowAsync((int)BookChapterComboBox.SelectedItem, version);
        }
        catch (Exception exception)
        {
            if (version != _loadVersion) return;
            _loadedBook = null;
            _readerWindow.Clear();
            _readerLines.Clear();
            NavigationError.Message = "This passage could not be loaded. Please select the book and chapter again.";
            NavigationError.IsOpen = true;
            System.Diagnostics.Debug.WriteLine(exception);
        }
        finally
        {
            if (version == _loadVersion) { NavigationLoading.Visibility = Visibility.Collapsed; _positioningReader = false; _changingBook = false; BookChapterComboBox.IsEnabled = _loadedBook is not null; }
        }
    }
    private async void RetryNavigation_Click(object sender, RoutedEventArgs args) =>
        await NavigateToBookAsync(_book ?? _catalog.Books[0], _settings.Chapter);

    private async void BookChapterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_changingBook || BookChapterComboBox.SelectedItem is not int chapter) return;
        var book = _pendingBook ?? _book;
        if (book is null) return;
        _pendingBook = null;
        await NavigateToBookAsync(book, chapter);
    }
    private async void ToggleSearch()
    {
        await SetSearchVisibilityAsync(SearchPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible);

        if (SearchPanel.Visibility == Visibility.Visible)
        {
            SearchBox.Focus(FocusState.Programmatic);
        }
    }

    private async void CloseSearchPanelButton_Click(object sender, RoutedEventArgs e)
    {
        await SetSearchVisibilityAsync(Visibility.Collapsed);
        SearchResultsListView.ItemsSource = null;
        ++_searchVersion;
        SearchBox.Text = string.Empty;
        SearchEmptyText.Visibility = Visibility.Collapsed;
        SearchNavigationItem.Focus(FocusState.Programmatic);
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e) => SearchBible();

    private void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            SearchBible();
        }
    }

    private async void SearchBible()
    {
        ++_searchVersion;
        var query = SearchBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(query) || _loadedBook is null)
        {
            SearchResultsListView.ItemsSource = null;
            SearchEmptyText.Visibility = Visibility.Collapsed;
            return;
        }

        var version = ++_searchVersion;
        var scope = SearchScopeComboBox.SelectedIndex;
        var matches = new List<SearchResultRow>();
        SearchEmptyText.Text = "Searching…";
        SearchEmptyText.Visibility = Visibility.Visible;
        SearchResultsListView.ItemsSource = null;
        try
        {
            foreach (var book in _catalog.Books.Where(book => scope == 0 || (scope == 1 ? (int)book.Id <= 39 : (int)book.Id > 39)))
            {
                SearchEmptyText.Text = $"Searching {book.Title}…";
                await Task.Delay(1);
                if (version != _searchVersion) return;
                var content = await _catalog.LoadAsync(book);
                foreach (var chapter in content.Chapters)
                    foreach (var verse in _catalog.DisplayLines(chapter).Where(verse => verse.IsVerse))
                    {
                        var text = verse.Text;
                        if (text.Contains(query, StringComparison.OrdinalIgnoreCase))
                            matches.Add(new SearchResultRow(book, chapter.Index, verse.VerseNumber, $"{book.Title} {chapter.Index}:{verse.VerseNumber}", text));
                    }
            }
        }
        catch (Exception exception)
        {
            SearchEmptyText.Text = $"Search could not complete: {exception.Message}";
            return;
        }
        if (version != _searchVersion) return;
        SearchEmptyText.Text = "No verses found.";
        SearchResultsListView.ItemsSource = matches;
        SearchEmptyText.Visibility = matches.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void SearchResultsListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not SearchResultRow result)
        {
            return;
        }

        await SetSearchVisibilityAsync(Visibility.Collapsed);
        await NavigateToBookAsync(result.Book, result.Chapter);
        ScrollToVerse(result.Verse);
    }

    private static string PlainText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return System.Text.RegularExpressions.Regex.Replace(
            System.Net.WebUtility.HtmlDecode(text), "<[^>]+>", string.Empty);
    }

    private sealed class SearchResultRow
    {
        public Services.ScriptureBook Book { get; }
        public int Chapter { get; }
        public int Verse { get; }
        public string ReferenceText { get; }
        public string VerseText { get; }

        public SearchResultRow(Services.ScriptureBook book, int chapter, int verse, string referenceText, string verseText)
        {
            Book = book;
            Chapter = chapter;
            Verse = verse;
            ReferenceText = referenceText;
            VerseText = verseText;
        }
    }

    private sealed class ReaderLine : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        public Microsoft.UI.Xaml.Media.Brush? HighlightBrush { get; private set; }
        public void Highlight(bool active)
        {
            HighlightBrush = active ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(65, 80, 160, 240)) : null;
            PropertyChanged?.Invoke(this, new(nameof(HighlightBrush)));
        }
        public string Text { get; }
        public string Number { get; }
        public Visibility VerseVisibility { get; }
        public Visibility HeadingVisibility { get; }
        public Visibility PoetryVisibility { get; }
        public Visibility TextVisibility { get; }
        public Visibility FootnoteVisibility { get; }
        public Visibility ParagraphVisibility { get; }
        public Visibility NumberVisibility { get; }
        public double BodySize { get; } = 18;
        public double NumberSize => BodySize * 0.7;
        public double HeadingSize { get; private set; }
        public Thickness HeadingMargin { get; private set; } = new(0, 14, 0, 8);
        public Microsoft.UI.Xaml.Media.FontFamily ReaderFont { get; } = new("ms-appx:///Assets/Fonts/SegoeUI.ttf#Segoe UI");
        public Microsoft.UI.Xaml.Media.FontFamily HeadingFont { get; private set; } = new("ms-appx:///Assets/Fonts/SegoeUIBold.ttf#Segoe UI");

        public ReaderLine(ChapterDisplayLine line, Elementary.Core.Interfaces.ISettings settings)
        {
            BodySize = settings.FontSize == Elementary.Core.Enums.EFontSize.Small ? 14 : settings.FontSize == Elementary.Core.Enums.EFontSize.Large ? 22 : 18;
            HeadingSize = BodySize * 1.2;
            ReaderFont = new(settings.Font == Elementary.Core.Enums.EFont.Georgia ? "ms-appx:///Assets/Fonts/Georgia.ttf#Georgia" : "ms-appx:///Assets/Fonts/SegoeUI.ttf#Segoe UI");
            HeadingFont = new(settings.Font == Elementary.Core.Enums.EFont.Georgia ? "ms-appx:///Assets/Fonts/GeorgiaBold.ttf#Georgia" : "ms-appx:///Assets/Fonts/SegoeUIBold.ttf#Segoe UI");
            NumberVisibility = ToVisibility(settings.ShowVerseNumbers ?? true);
            Text = line.Text ?? string.Empty;
            Number = line.VerseNumberText;
            VerseVisibility = ToVisibility(line.IsVerse);
            HeadingVisibility = ToVisibility(line.IsHeading);
            PoetryVisibility = ToVisibility(line.IsPoetry);
            TextVisibility = ToVisibility(line.IsText);
            FootnoteVisibility = ToVisibility(line.IsFootnote);
            ParagraphVisibility = ToVisibility(line.IsParagraphBreak);
        }

        private ReaderLine(string text)
        {
            Text = text;
            Number = string.Empty;
            VerseVisibility = Visibility.Collapsed;
            HeadingVisibility = Visibility.Collapsed;
            PoetryVisibility = Visibility.Collapsed;
            TextVisibility = Visibility.Visible;
            FootnoteVisibility = Visibility.Collapsed;
            ParagraphVisibility = Visibility.Collapsed;
        }

        public static ReaderLine CreateText(string text) => new(text);

        public static ReaderLine CreateSection(string text, Elementary.Core.Interfaces.ISettings settings, bool book)
        {
            var line = new ReaderLine(new ChapterDisplayLine { Type = ChapterDisplayLineType.Heading, Text = text }, settings);
            line.HeadingSize = line.BodySize * (book ? 1.9 : 1.55);
            line.HeadingMargin = new Thickness(0, book ? 48 : 24, 0, 8);
            return line;
        }

        private static Visibility ToVisibility(bool value) => value ? Visibility.Visible : Visibility.Collapsed;
    }
}
