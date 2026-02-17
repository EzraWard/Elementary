using Elementary.Core.Models;
using Elementary.ViewModels;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using System.Text.RegularExpressions;
using System.Net;
using Windows.UI.Text;

namespace Elementary
{
    public sealed partial class BiblePage : Page
    {
        public BiblePageViewModel _viewModel;
        public bool _isLoaded;
        private double _previousVerticalOffset = 0;
        private bool _isUpdatingFromScroll = false;

        public BiblePage()
        {
            InitializeComponent();
            Loaded += BiblePage_Loaded;

            _viewModel = new BiblePageViewModel();
        }

        private async void BiblePage_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = _viewModel;
            await _viewModel.Initialize();

            _isLoaded = true;
            
            // Only scroll if current chapter is not the first in the list
            var currentChapterIndex = _viewModel.Chapters.IndexOf(_viewModel.CurrentChapter);
            if (currentChapterIndex > 0)
            {
                // Give UI time to layout
                await System.Threading.Tasks.Task.Delay(100);
                ScrollToCurrentChapter();
            }
        }

        private void ScrollToCurrentChapter()
        {
            if (_viewModel?.Chapters == null || _viewModel.Chapters.Count == 0) return;

            BibleScrollViewer.UpdateLayout();
            
            var currentChapterIndex = _viewModel.Chapters.IndexOf(_viewModel.CurrentChapter);
            if (currentChapterIndex > 0)
            {
                double scrollPosition = 0;
                for (int i = 0; i < currentChapterIndex; i++)
                {
                    var element = ChaptersRepeater.TryGetElement(i);
                    if (element is Grid grid)
                    {
                        scrollPosition += grid.ActualHeight + 32;
                    }
                }
                BibleScrollViewer.ChangeView(null, scrollPosition, null, true);
            }
            else
            {
                BibleScrollViewer.ChangeView(null, 0, null, true);
            }

            // Ensure first chapter has enough top offset when necessary
            ApplyTopOffsetToFirstChapter();
        }

        private void ApplyTopOffsetToFirstChapter()
        {
            try
            {
                var firstElement = ChaptersRepeater.TryGetElement(0) as Grid;
                if (firstElement == null) return;

                var firstChapter = _viewModel?.Chapters?.Count > 0 ? _viewModel.Chapters[0] : null;
                if (firstChapter != null)
                {
                    var book = _viewModel?.Bible?.Books?.FirstOrDefault(b => b.Chapters.Contains(firstChapter));
                    if (book != null && firstChapter.Index == 1)
                    {
                        double offset = 0;
                        if (ChooserBorder != null)
                        {
                            offset = ChooserBorder.ActualHeight + ChooserBorder.Margin.Top + ChooserBorder.Padding.Top + 8;
                        }
                        firstElement.Margin = new Thickness(0, offset, 0, 0);
                        return;
                    }
                }
                firstElement.Margin = new Thickness(0);
            }
            catch
            {
                // ignore errors
            }
        }

        private void ChapterItemGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var grid = (Grid)sender;
            var richTextBlock = grid.Children[0] as RichTextBlock;
            if (richTextBlock == null) return;

            // Set font properties from ViewModel
            richTextBlock.FontFamily = new Windows.UI.Xaml.Media.FontFamily(_viewModel.Font);
            richTextBlock.FontSize = _viewModel.FontSize;

            // Populate content from bound Chapter
            try
            {
                var chapter = grid.DataContext as Chapter;
                if (chapter != null)
                {
                    PopulateRichTextBlock(richTextBlock, chapter.ChapterText);
                }
            }
            catch
            {
                // ignore rendering errors
            }

            var gridWidth = grid.ActualWidth;

            if (gridWidth > 750)
            {
                richTextBlock.Width = 700;
                // If this is the first element, ensure offset is applied after size settles
                var first = ChaptersRepeater.TryGetElement(0);
                if (object.ReferenceEquals(grid, first)) ApplyTopOffsetToFirstChapter();
                return;
            }
            if (gridWidth < 350)
            {
                richTextBlock.Width = 300;
                var first = ChaptersRepeater.TryGetElement(0);
                if (object.ReferenceEquals(grid, first)) ApplyTopOffsetToFirstChapter();
                return;
            }

            richTextBlock.Width = gridWidth - 50;
            var firstElement = ChaptersRepeater.TryGetElement(0);
            if (object.ReferenceEquals(grid, firstElement)) ApplyTopOffsetToFirstChapter();
        }

        private void UpdateCurrentChapterFromScroll()
        {
            if (!_isLoaded || _viewModel.Chapters.Count == 0) return;

            // Find which chapter is currently most visible in the viewport
            try
            {
                var scrollViewer = BibleScrollViewer;
                var viewportHeight = scrollViewer.ViewportHeight;

                Chapter mostVisibleChapter = null;
                double maxVisibleArea = 0;

                for (int i = 0; i < _viewModel.Chapters.Count; i++)
                {
                    var element = ChaptersRepeater.TryGetElement(i);
                    if (element is FrameworkElement frameworkElement)
                    {
                        // Get element position relative to scrollviewer viewport (not content)
                        var transform = frameworkElement.TransformToVisual(scrollViewer);
                        var elementPosition = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
                        var elementTop = elementPosition.Y;
                        var elementBottom = elementTop + frameworkElement.ActualHeight;

                        // Calculate how much of this element is visible in viewport (0 to viewportHeight)
                        var visibleTop = Math.Max(elementTop, 0);
                        var visibleBottom = Math.Min(elementBottom, viewportHeight);
                        var visibleArea = Math.Max(0, visibleBottom - visibleTop);

                        // Track which element has the most visible area
                        if (visibleArea > maxVisibleArea)
                        {
                            maxVisibleArea = visibleArea;
                            mostVisibleChapter = _viewModel.Chapters[i];
                        }
                    }
                }

                // Update the ViewModel's current chapter if different and we found a visible chapter
                if (mostVisibleChapter != null && _viewModel.CurrentChapter != mostVisibleChapter && maxVisibleArea > 50)
                {
                    _isUpdatingFromScroll = true;
                    _viewModel.UpdateCurrentChapterFromScroll(mostVisibleChapter);
                    _isUpdatingFromScroll = false;
                }
            }
            catch
            {
                // Ignore errors during visual tree traversal
            }
        }

        private void BibleScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (!_isLoaded) return;

            var scrollViewer = (ScrollViewer)sender;
            var verticalOffset = scrollViewer.VerticalOffset;
            var maxVerticalOffset = scrollViewer.ScrollableHeight;

            // Load next chapter when scrolling down near bottom
            if (verticalOffset > _previousVerticalOffset && maxVerticalOffset - verticalOffset < 500)
            {
                _viewModel.LoadNextChapter();
            }
            // Load previous chapter when scrolling up near top
            else if (verticalOffset < _previousVerticalOffset && verticalOffset < 500)
            {
                // Store the current scroll position to restore after adding content at top
                var oldScrollableHeight = scrollViewer.ScrollableHeight;
                _viewModel.LoadPreviousChapter();
                
                // Adjust scroll position to maintain view (needs to be done after layout updates)
                scrollViewer.UpdateLayout();
                var newScrollableHeight = scrollViewer.ScrollableHeight;
                var heightDifference = newScrollableHeight - oldScrollableHeight;
                if (heightDifference > 0)
                {
                    scrollViewer.ChangeView(null, verticalOffset + heightDifference, null, true);
                }
            }

            // Update the current chapter based on scroll position
            if (!e.IsIntermediate)
            {
                UpdateCurrentChapterFromScroll();
            }

            _previousVerticalOffset = verticalOffset;
        }

        // Populate a RichTextBlock from simplified HTML created by the USFM parser.
        private void PopulateRichTextBlock(RichTextBlock rtb, string html)
        {
            rtb.Blocks.Clear();

            if (string.IsNullOrEmpty(html))
            {
                rtb.Blocks.Add(new Paragraph());
                return;
            }

            var decoded = WebUtility.HtmlDecode(html);

            // Tokenize into tags and text
            var parts = Regex.Matches(decoded, "(<[^>]+>|[^<]+)", RegexOptions.Singleline);

            Paragraph currentPara = null;
            var styleStack = new Stack<(bool italic, bool bold)>();
            styleStack.Push((false, false));

            void StartParagraph(Paragraph p)
            {
                if (p == null) return;
                currentPara = p;
                rtb.Blocks.Add(currentPara);
            }

            foreach (Match part in parts)
            {
                var token = part.Value;
                if (token.StartsWith("<"))
                {
                    var tag = token.Trim('<', '>', ' ', '\t', '\r', '\n');
                    var tagLower = tag.ToLowerInvariant();

                    if (tagLower.StartsWith("p") || tagLower == "/p")
                    {
                        if (!tagLower.StartsWith("/")) StartParagraph(new Paragraph());
                        else currentPara = null;
                        continue;
                    }

                    if (tagLower.StartsWith("h1") || tagLower.StartsWith("h"))
                    {
                        var h = new Paragraph { FontWeight = FontWeights.Bold, FontSize = rtb.FontSize * 1.15 };
                        StartParagraph(h);
                        continue;
                    }

                    if (tagLower.StartsWith("quote") || tagLower.StartsWith("div class=\"q\""))
                    {
                        var bq = new Paragraph { Margin = new Thickness(20,0,0,0) };
                        StartParagraph(bq);
                        continue;
                    }

                    if (tagLower == "br")
                    {
                        StartParagraph(new Paragraph());
                        continue;
                    }

                    // superscript tags create an InlineUIContainer hosting a small TextBlock
                    if (tagLower.StartsWith("sup") && !tagLower.StartsWith("/"))
                    {
                        // extract number if present inside tag like <sup>1</sup> will be handled when text token appears, so just set a marker by adding nothing
                        continue;
                    }
                    if (tagLower.StartsWith("/sup")) { continue; }

                    if (tagLower.StartsWith("em") && !tagLower.StartsWith("/")) { var top = styleStack.Peek(); styleStack.Push((true, top.bold)); continue; }
                    if (tagLower.StartsWith("/em") || tagLower.StartsWith("/it")) { if (styleStack.Count>1) styleStack.Pop(); continue; }
                    if (tagLower.StartsWith("b") && !tagLower.StartsWith("/")) { var top = styleStack.Peek(); styleStack.Push((top.italic, true)); continue; }
                    if (tagLower.StartsWith("/b") || tagLower.StartsWith("/bd")) { if (styleStack.Count>1) styleStack.Pop(); continue; }

                    // footnote tag <fn id="n"/> - render a small superscript marker
                    if (tagLower.StartsWith("fn ") || tagLower.StartsWith("fn") )
                    {
                        // extract id
                        var idMatch = Regex.Match(tag, "id=\"(\\d+)\"");
                        if (idMatch.Success)
                        {
                            var id = idMatch.Groups[1].Value;
                            if (currentPara == null) StartParagraph(new Paragraph());
                            var tb = new TextBlock { Text = id, FontSize = rtb.FontSize * 0.7, Margin = new Thickness(0,-6,2,0) };
                            var container = new InlineUIContainer { Child = tb };
                            currentPara.Inlines.Add(container);
                        }
                        continue;
                    }

                    if (tagLower.StartsWith("xr") && !tagLower.StartsWith("/"))
                    {
                        // open crossref span (we'll render as parentheses)
                        if (currentPara == null) StartParagraph(new Paragraph());
                        currentPara.Inlines.Add(new Run { Text = " (", FontStyle = FontStyle.Normal });
                        continue;
                    }
                    if (tagLower.StartsWith("/xr"))
                    {
                        if (currentPara == null) StartParagraph(new Paragraph());
                        currentPara.Inlines.Add(new Run { Text = ")", FontStyle = FontStyle.Normal });
                        continue;
                    }

                    // unknown tag: ignore
                    continue;
                }

                // Text token
                var text = token;
                if (currentPara == null) StartParagraph(new Paragraph());

                // If the text contains a simple <sup>number</sup> pattern, handle with InlineUIContainer
                var supMatch = Regex.Match(text, "<sup>(\\d+)</sup>", RegexOptions.IgnoreCase);
                if (supMatch.Success)
                {
                    var num = supMatch.Groups[1].Value;
                    // Add superscript number
                    var tb = new TextBlock { Text = num, FontSize = rtb.FontSize * 0.75, Margin = new Thickness(0,-6,4,0) };
                    currentPara.Inlines.Add(new InlineUIContainer { Child = tb });

                    // Append the remaining text after the sup
                    var after = text.Substring(supMatch.Index + supMatch.Length).TrimStart();
                    if (!string.IsNullOrEmpty(after))
                    {
                        var top = styleStack.Peek();
                        var run = new Run { Text = after + " " };
                        if (top.italic) run.FontStyle = FontStyle.Italic;
                        if (top.bold) run.FontWeight = FontWeights.Bold;
                        currentPara.Inlines.Add(run);
                    }

                    continue;
                }

                var topStyle = styleStack.Peek();
                var run2 = new Run { Text = text };
                if (topStyle.italic) run2.FontStyle = FontStyle.Italic;
                if (topStyle.bold) run2.FontWeight = FontWeights.Bold;
                currentPara.Inlines.Add(run2);
            }
        }

        private void BibleBookChapterComboBoxes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded || _isUpdatingFromScroll) return;

            // If the book ComboBox triggered this, reset chapter selection to 1 so the chapter ComboBox reflects the new book
            if (sender == BibleBookComboBox)
            {
                _viewModel.SelectedChapterIndex = 1;
            }

            // Save manual selection to navigation history (only on explicit combobox selection)
            try
            {
                var settingsService = App.Services.GetRequiredService<Core.Interfaces.ISettingsService>();
                var history = settingsService.GetNavigationHistory() ?? new System.Collections.Generic.List<Core.Models.NavigationHistoryItem>();
                var currentBookTitle = _viewModel.CurrentBook?.Title ?? _viewModel.Bible?.Books?.FirstOrDefault()?.Title ?? string.Empty;
                var currentChapter = _viewModel.SelectedChapterIndex;
                // Avoid duplicate consecutive entries
                if (history.Count == 0 || history[history.Count - 1].BookTitle != currentBookTitle || history[history.Count - 1].Chapter != currentChapter)
                {
                    history.Add(new Core.Models.NavigationHistoryItem { BookTitle = currentBookTitle, Chapter = currentChapter });
                    // keep max 10, remove oldest if necessary
                    if (history.Count > 10) history.RemoveAt(0);
                    settingsService.SaveNavigationHistory(history);
                }
            }
            catch
            {
                // ignore errors saving history
            }

            // Reload chapters from the selected position
            _viewModel.LoadInitialChapters();
            
            // Scroll to the current chapter
            ScrollToCurrentChapter();
        }

        public void NavigateToFromHistory(string bookTitle, int chapter)
        {
            // If page isn't fully initialized yet, defer until Loaded finishes
            if (!_isLoaded)
            {
                Loaded += (s, e) =>
                {
                    _viewModel.UpdateNavigationSettings(bookTitle, chapter);
                    _viewModel.LoadInitialChapters();
                    ScrollToCurrentChapter();
                };
                return;
            }

            _viewModel.UpdateNavigationSettings(bookTitle, chapter);
            _viewModel.LoadInitialChapters();
            ScrollToCurrentChapter();
        }

        // Show only the provided chapters in the Bible view (used by Reading Plan)
        public void ShowChapters(List<Chapter> chapters)
        {
            if (chapters == null || chapters.Count == 0) return;

            // If page isn't initialized yet, wait until Loaded completes
            if (!_isLoaded)
            {
                Loaded += (s, e) =>
                {
                    _viewModel.Chapters = new System.Collections.ObjectModel.ObservableCollection<Chapter>(chapters);
                    _viewModel.CurrentChapter = chapters[0];
                    ScrollToCurrentChapter();
                };
                return;
            }

            _viewModel.Chapters = new System.Collections.ObjectModel.ObservableCollection<Chapter>(chapters);
            _viewModel.CurrentChapter = chapters[0];
            ScrollToCurrentChapter();
        }
    }
}