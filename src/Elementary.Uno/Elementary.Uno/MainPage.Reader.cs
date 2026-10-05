using Elementary.Core.Dictionaries;
using Elementary.Core.Parsers;
using Elementary.Uno.Services;
using Windows.Foundation;

namespace Elementary.Uno;

public sealed partial class MainPage
{
    private sealed record ReaderChapter(ScriptureBook Book, UsfmChapter Chapter, List<ReaderLine> Lines);
    private readonly List<ReaderChapter> _readerWindow = new();
    private ScriptureBook? _pendingBook;
    private bool _updatingWindow;
    private bool _positioningReader;
    private DateTime _ignoreScrollUntil;
    private int _readerLayoutVersion;
    private bool _readerNeedsPosition;

    private async Task SetSearchVisibilityAsync(Visibility visibility)
    {
        if (SearchPanel.Visibility == visibility) return;
        var anchor = _readerLines.FirstOrDefault(line => RowTop(line) >= 68);
        var top = anchor is null ? double.NaN : RowTop(anchor);
        SearchPanel.Visibility = visibility;
        if (anchor is not null) await RestoreReaderAnchorAsync(anchor, top);
    }

    private async Task RestoreReaderAnchorAsync(ReaderLine anchor, double top)
    {
        var layoutVersion = ++_readerLayoutVersion;
        var loadVersion = _loadVersion;
        _positioningReader = true;
        try
        {
            for (var pass = 0; pass < 3; pass++)
            {
                await Task.Delay(60);
                if (loadVersion != _loadVersion || layoutVersion != _readerLayoutVersion) return;
                VerseList.UpdateLayout();
                var after = RowTop(anchor);
                if (!double.IsNaN(top) && !double.IsNaN(after))
                    ReaderScrollViewer.ChangeView(null, Math.Max(0, ReaderScrollViewer.VerticalOffset + after - top), null, true);
            }
            _ignoreScrollUntil = DateTime.UtcNow.AddMilliseconds(200);
        }
        finally
        {
            if (loadVersion == _loadVersion && layoutVersion == _readerLayoutVersion) _positioningReader = false;
        }
    }

    private void SynchronizePickers()
    {
        if (_book is null || _pendingBook is not null) return;
        _changingBook = true;
        BibleBookComboBox.SelectedItem = _book;
        BookChapterComboBox.ItemsSource = Enumerable.Range(1, EBookToLocation.EBookToChapterCount[_book.Id]).ToList();
        BookChapterComboBox.SelectedItem = _settings.Chapter;
        _changingBook = false;
    }

    private void BookChapterComboBox_DropDownClosed(object sender, object args)
    {
        if (_pendingBook is null) return;
        _pendingBook = null;
        SynchronizePickers();
    }

    private async Task<ReaderChapter?> AdjacentChapterAsync(ReaderChapter chapter, int direction)
    {
        var bookIndex = _catalog.Books.ToList().IndexOf(chapter.Book);
        var number = chapter.Chapter.Index + direction;
        var book = chapter.Book;
        if (number < 1 || number > EBookToLocation.EBookToChapterCount[book.Id])
        {
            bookIndex += direction;
            if (bookIndex < 0 || bookIndex >= _catalog.Books.Count) return null;
            book = _catalog.Books[bookIndex];
            number = direction > 0 ? 1 : EBookToLocation.EBookToChapterCount[book.Id];
        }
        var content = await _catalog.LoadAsync(book);
        return CreateReaderChapter(book, content.Chapters.Single(item => item.Index == number));
    }

    private ReaderChapter CreateReaderChapter(ScriptureBook book, UsfmChapter chapter)
    {
        var lines = new List<ReaderLine>();
        if (chapter.Index == 1) lines.Add(ReaderLine.CreateSection(book.Title, _settings, true));
        lines.Add(ReaderLine.CreateSection($"Chapter {chapter.Index}", _settings, false));
        lines.AddRange(_catalog.DisplayLines(chapter).Select(line => new ReaderLine(line, _settings)));
        return new ReaderChapter(book, chapter, lines);
    }

    private FrameworkElement? RowElement(ReaderLine line) =>
        VerseList.ContainerFromIndex(_readerLines.IndexOf(line)) as FrameworkElement;

    private double RowTop(ReaderLine line) => RowElement(line)?.TransformToVisual(ReaderScrollViewer).TransformPoint(new Point()).Y ?? double.NaN;

    private async Task ResetReaderWindowAsync(int chapterNumber, int version)
    {
        _positioningReader = true;
        try
        {
            var current = CreateReaderChapter(_book!, _loadedBook!.Chapters.Single(item => item.Index == chapterNumber));
            var previous = await AdjacentChapterAsync(current, -1);
            var next = await AdjacentChapterAsync(current, 1);
            if (version != _loadVersion) return;
            _readerWindow.Clear();
            if (previous is not null) _readerWindow.Add(previous);
            _readerWindow.Add(current);
            if (next is not null) _readerWindow.Add(next);
            _readerLines.Clear();
            foreach (var item in _readerWindow) foreach (var line in item.Lines) _readerLines.Add(line);
            // Font metrics and the ScrollViewer extent can settle on different frames.
            for (var pass = 0; pass < 3; pass++)
            {
                await Task.Delay(60);
                if (version != _loadVersion) return;
                VerseList.UpdateLayout();
                var top = RowTop(current.Lines[0]);
                if (!double.IsNaN(top)) ReaderScrollViewer.ChangeView(null, Math.Max(0, ReaderScrollViewer.VerticalOffset + top - 68), null, true);
            }
            _ignoreScrollUntil = DateTime.UtcNow.AddMilliseconds(200);
        }
        finally { if (version == _loadVersion) _positioningReader = false; }
    }

    private async void ReaderScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs args)
    {
        if (_positioningReader || _updatingWindow || _readerWindow.Count == 0 || DateTime.UtcNow < _ignoreScrollUntil) return;
        SynchronizeReadingLocation();
        if (args.IsIntermediate) return;
        _updatingWindow = true;
        var version = _loadVersion;
        try
        {
            var prepend = ReaderScrollViewer.VerticalOffset < 900;
            var append = ReaderScrollViewer.ScrollableHeight - ReaderScrollViewer.VerticalOffset < 900;
            if (!prepend && !append) return;
            var edge = prepend ? _readerWindow[0] : _readerWindow[^1];
            var adjacent = await AdjacentChapterAsync(edge, prepend ? -1 : 1);
            if (adjacent is null || version != _loadVersion) return;
            var anchor = _readerWindow.SelectMany(item => item.Lines).FirstOrDefault(line => RowTop(line) >= 0) ?? edge.Lines[0];
            var before = RowTop(anchor);
            if (prepend)
            {
                _readerWindow.Insert(0, adjacent);
                for (var i = adjacent.Lines.Count - 1; i >= 0; i--) _readerLines.Insert(0, adjacent.Lines[i]);
            }
            else
            {
                _readerWindow.Add(adjacent);
                foreach (var line in adjacent.Lines) _readerLines.Add(line);
            }
            // Keep a bounded window of chapters; never retain the entire Bible's UI.
            if (_readerWindow.Count > 7)
            {
                var removed = prepend ? _readerWindow[^1] : _readerWindow[0];
                _readerWindow.Remove(removed);
                foreach (var line in removed.Lines) _readerLines.Remove(line);
            }
            // Preserve the same visible line across layout/scroll anchoring updates.
            // A single synchronous UpdateLayout is insufficient on the Skia renderer.
            for (var pass = 0; pass < 3; pass++)
            {
                await Task.Delay(60);
                if (version != _loadVersion) return;
                VerseList.UpdateLayout();
                var after = RowTop(anchor);
                if (!double.IsNaN(before) && !double.IsNaN(after))
                    ReaderScrollViewer.ChangeView(null, Math.Max(0, ReaderScrollViewer.VerticalOffset + after - before), null, true);
            }
            SynchronizeReadingLocation();
            _ignoreScrollUntil = DateTime.UtcNow.AddMilliseconds(100);
        }
        catch (Exception exception) { System.Diagnostics.Debug.WriteLine($"Unable to extend reader: {exception}"); }
        finally { _updatingWindow = false; }
    }

    private void SynchronizeReadingLocation()
    {
        var current = _readerWindow.LastOrDefault(item => RowTop(item.Lines[0]) <= 100) ?? _readerWindow[0];
        if (_book == current.Book && _settings.Chapter == current.Chapter.Index) return;
        _book = current.Book;
        // Scrolling updates the saved position, without adding every passed chapter to history.
        _settings.Book = current.Book.Id;
        _settings.Chapter = current.Chapter.Index;
        _currentLocation = new() { BookKey = current.Book.Key, BookTitle = current.Book.Title, Chapter = current.Chapter.Index };
        _settingsService.SaveSettings(_settings);
        SynchronizePickers();
    }

    private void RefreshReaderTypography()
    {
        if (_readerWindow.Count == 0) return;
        if (_currentPage != "Bible") { _readerNeedsPosition = true; return; }
        var anchorIndex = _readerLines.ToList().FindIndex(line => RowTop(line) >= 68);
        var top = anchorIndex < 0 ? double.NaN : RowTop(_readerLines[anchorIndex]);
        for (var i = 0; i < _readerWindow.Count; i++)
            _readerWindow[i] = CreateReaderChapter(_readerWindow[i].Book, _readerWindow[i].Chapter);
        _readerLines.Clear();
        foreach (var chapter in _readerWindow) foreach (var line in chapter.Lines) _readerLines.Add(line);
        if (anchorIndex >= 0 && anchorIndex < _readerLines.Count)
            _ = RestoreReaderAnchorAsync(_readerLines[anchorIndex], top);
    }
}
