using Elementary.Core.Models;
using Elementary.Core.Parsers;
using Microsoft.UI.Xaml.Input;
using System.Collections.ObjectModel;
using System.Reflection;

namespace Elementary.Uno;

public sealed partial class MainPage : Page
{
    private const string GenesisResourceName = "Elementary.Uno.Scripture.Genesis.usfm";
    private readonly ObservableCollection<ReaderLine> _readerLines = new();
    private UsfmBook? _genesis;

    public MainPage()
    {
        InitializeComponent();
        InitializeShell();
        VerseList.ItemsSource = _readerLines;
        Loaded += MainPage_Loaded;
    }

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= MainPage_Loaded;

        try
        {
            await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(GenesisResourceName)
                ?? throw new InvalidOperationException($"The embedded scripture resource '{GenesisResourceName}' was not found.");
            using var reader = new StreamReader(stream);
            var usfm = await reader.ReadToEndAsync();
            _genesis = UsfmParser.ParseBook(usfm);

            if (_genesis?.Chapters.Count is not > 0)
            {
                throw new InvalidOperationException("Genesis did not contain any readable chapters.");
            }

            BookChapterComboBox.ItemsSource = _genesis.Chapters.Select(chapter => chapter.Index).ToList();
            BookChapterComboBox.SelectedItem = Math.Clamp(_settings.Chapter, 1, _genesis.Chapters.Count);
            ApplyReaderSettings();
        }
        catch (Exception exception)
        {
            ChapterHeading.Text = "Genesis";
            _readerLines.Clear();
            _readerLines.Add(ReaderLine.CreateText($"Scripture could not be loaded: {exception.Message}"));
        }
    }

    private void BibleBookComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Genesis is the first bundled-book slice. The selector is already in place for
        // the full translation catalog when that content is added.
    }

    private void BookChapterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_genesis is null || BookChapterComboBox.SelectedItem is not int chapterNumber)
        {
            return;
        }

        var chapter = _genesis.Chapters.FirstOrDefault(item => item.Index == chapterNumber);
        if (chapter is null)
        {
            return;
        }

        ChapterHeading.Text = $"Chapter {chapter.Index}";
        RecordLocation(chapter.Index);
        _readerLines.Clear();
        foreach (var line in chapter.ToDisplayLines())
        {
            _readerLines.Add(new ReaderLine(line, _settings));
        }

        ReaderScrollViewer.ChangeView(null, 0, null);
    }

    private void ToggleSearch()
    {
        SearchPanel.Visibility = SearchPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;

        if (SearchPanel.Visibility == Visibility.Visible)
        {
            SearchBox.Focus(FocusState.Programmatic);
        }
    }

    private void CloseSearchPanelButton_Click(object sender, RoutedEventArgs e)
    {
        SearchPanel.Visibility = Visibility.Collapsed;
        SearchResultsListView.ItemsSource = null;
        SearchBox.Text = string.Empty;
        SearchEmptyText.Visibility = Visibility.Collapsed;
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e) => SearchGenesis();

    private void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            SearchGenesis();
        }
    }

    private void SearchGenesis()
    {
        var query = SearchBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(query) || _genesis is null)
        {
            SearchResultsListView.ItemsSource = null;
            SearchEmptyText.Visibility = Visibility.Collapsed;
            return;
        }

        var matches = _genesis.Chapters
            .SelectMany(chapter => chapter.Verses
                .Where(verse => verse.Number > 0
                    && PlainText(verse.Text).Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(verse => new SearchResultRow(
                    chapter.Index,
                    verse.Number,
                    $"Genesis {chapter.Index}:{verse.Number}",
                    PlainText(verse.Text))))
            .ToList();

        SearchResultsListView.ItemsSource = matches;
        SearchEmptyText.Visibility = matches.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SearchResultsListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not SearchResultRow result)
        {
            return;
        }

        BookChapterComboBox.SelectedItem = result.Chapter;
        SearchPanel.Visibility = Visibility.Collapsed;
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
        public int Chapter { get; }
        public int Verse { get; }
        public string ReferenceText { get; }
        public string VerseText { get; }

        public SearchResultRow(int chapter, int verse, string referenceText, string verseText)
        {
            Chapter = chapter;
            Verse = verse;
            ReferenceText = referenceText;
            VerseText = verseText;
        }
    }

    private sealed class ReaderLine
    {
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
        public double HeadingSize => BodySize * 1.2;
        public Microsoft.UI.Xaml.Media.FontFamily ReaderFont { get; } = new("ms-appx:///Assets/Fonts/SegoeUI.ttf#Segoe UI");

        public ReaderLine(ChapterDisplayLine line, Elementary.Core.Interfaces.ISettings settings)
        {
            BodySize = settings.FontSize == Elementary.Core.Enums.EFontSize.Small ? 14 : settings.FontSize == Elementary.Core.Enums.EFontSize.Large ? 22 : 18;
            ReaderFont = new(settings.Font == Elementary.Core.Enums.EFont.Georgia ? "ms-appx:///Assets/Fonts/Georgia.ttf#Georgia" : "ms-appx:///Assets/Fonts/SegoeUI.ttf#Segoe UI");
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

        private static Visibility ToVisibility(bool value) => value ? Visibility.Visible : Visibility.Collapsed;
    }
}
