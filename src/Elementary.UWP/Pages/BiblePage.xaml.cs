using Elementary.Core.Models;
using Elementary.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Elementary
{
    public sealed partial class BiblePage : Page
    {
        private const double ScrollAnchorY = 120d;
        private const int LayoutSettleDelayMs = 100;
        private const int ComboResetMaxAttempts = 5;
        private const int ComboResetRetryDelayMs = 20;

        private readonly BiblePageViewModel _viewModel;
        private bool _isLoaded;
        private bool _isInitializing;
        private bool _isUpdatingFromScroll;
        private bool _isAwaitingChapterSelection;
        private bool _suppressComboHandling;
        private DateTimeOffset _ignoreScrollSyncUntil = DateTimeOffset.MinValue;
        private readonly TranslateTransform _chooserTranslate = new TranslateTransform();
        private string _pendingHistoryBook;
        private int _pendingHistoryChapter;
        private string _pendingHistoryBookKey;
        private SearchNavigationParameter _pendingSearchParam;
        private FrameworkElement _highlightedElement;

        public BiblePage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Disabled;

            _viewModel = new BiblePageViewModel();
            DataContext = _viewModel;

            Loaded += BiblePage_Loaded;
            ChooserBorder.RenderTransform = _chooserTranslate;
            BibleScrollViewer.Opacity = 0;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is SearchNavigationParameter searchParam)
            {
                if (!_isLoaded)
                {
                    _pendingSearchParam = searchParam;
                    return;
                }

                _ = NavigateToFromSearchAsync(searchParam);
                return;
            }

            if (!(e.Parameter is NavigationHistoryItem historyItem)) return;

            if (!_isLoaded)
            {
                _pendingHistoryBook = historyItem.BookTitle;
                _pendingHistoryChapter = historyItem.Chapter;
                _pendingHistoryBookKey = historyItem.BookKey;
                return;
            }

            _ = NavigateToFromHistoryAsync(historyItem.BookTitle, historyItem.Chapter, historyItem.BookKey);
        }

        private async void BiblePage_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isInitializing || _isLoaded) return;

            _isInitializing = true;
            try
            {
                _viewModel.IsLoaded = false;
                BibleScrollViewer.Opacity = 0;

                await _viewModel.Initialize();
                _chooserTranslate.Y = 0;
                SetupTopFadeGradient();

                if (_pendingSearchParam != null)
                {
                    var pendingSearch = _pendingSearchParam;
                    _pendingSearchParam = null;

                    await _viewModel.UpdateNavigationSettingsAsync(pendingSearch.BookTitle, pendingSearch.ChapterIndex, pendingSearch.BookKey);
                    SynchronizePickerSelection();
                    await PositionReaderAsync(waitForLayout: true);

                    _viewModel.IsLoaded = true;
                    _isLoaded = true;
                    BibleScrollViewer.Opacity = 1;

                    await HighlightVerseAsync(pendingSearch.ChapterIndex, pendingSearch.VerseNumber);
                    return;
                }

                if (_pendingHistoryBook != null)
                {
                    var pendingBook = _pendingHistoryBook;
                    var pendingChapter = _pendingHistoryChapter;
                    var pendingBookKey = _pendingHistoryBookKey;
                    ClearPendingHistory();

                    await _viewModel.UpdateNavigationSettingsAsync(pendingBook, pendingChapter, pendingBookKey);
                }

                SynchronizePickerSelection();
                await PositionReaderAsync(waitForLayout: true);

                _viewModel.IsLoaded = true;
                _isLoaded = true;
                BibleScrollViewer.Opacity = 1;
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private void ScrollToCurrentChapter()
        {
            if (_viewModel?.Chapters == null || _viewModel.Chapters.Count == 0) return;

            BibleScrollViewer.UpdateLayout();

            var currentChapterIndex = _viewModel.Chapters.IndexOf(_viewModel.CurrentChapter);
            if (currentChapterIndex < 0)
            {
                return;
            }

            if (currentChapterIndex == 0)
            {
                BibleScrollViewer.ChangeView(null, 0, null, true);
            }
            else
            {
                var element = ChaptersRepeater.GetOrCreateElement(currentChapterIndex) as FrameworkElement;
                BibleScrollViewer.UpdateLayout();

                if (element != null)
                {
                    var transform = element.TransformToVisual(BibleScrollViewer);
                    var elementPosition = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
                    var targetOffset = Math.Max(0, BibleScrollViewer.VerticalOffset + elementPosition.Y);
                    BibleScrollViewer.ChangeView(null, targetOffset, null, true);
                }
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
                firstElement.Margin = new Thickness(0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying top offset: {ex.Message}");
            }
        }

        private void SetupTopFadeGradient()
        {
            Color baseColor;
            if (Application.Current.Resources.TryGetValue("ApplicationPageBackgroundThemeBrush", out var res)
                && res is SolidColorBrush bgBrush)
            {
                baseColor = bgBrush.Color;
            }
            else
            {
                var isDark = ActualTheme == ElementTheme.Dark ||
                             (ActualTheme == ElementTheme.Default && Application.Current.RequestedTheme == ApplicationTheme.Dark);
                baseColor = isDark ? Color.FromArgb(255, 32, 32, 32) : Color.FromArgb(255, 243, 243, 243);
            }

            // Gradient shadow layer
            var gradient = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(0, 1)
            };
            // Gradient shadow layer — lighter tint, extended fade to soften blur edge
            gradient.GradientStops.Add(new GradientStop { Color = Color.FromArgb(160, baseColor.R, baseColor.G, baseColor.B), Offset = 0 });
            gradient.GradientStops.Add(new GradientStop { Color = Color.FromArgb(120, baseColor.R, baseColor.G, baseColor.B), Offset = 0.55 });
            gradient.GradientStops.Add(new GradientStop { Color = Color.FromArgb(60, baseColor.R, baseColor.G, baseColor.B), Offset = 0.75 });
            gradient.GradientStops.Add(new GradientStop { Color = Color.FromArgb(20, baseColor.R, baseColor.G, baseColor.B), Offset = 0.9 });
            gradient.GradientStops.Add(new GradientStop { Color = Color.FromArgb(0, baseColor.R, baseColor.G, baseColor.B), Offset = 1.0 });
            TopFadeBorder.Background = gradient;

            // Blur layer behind the gradient
            BlurBorder.Background = new AcrylicBrush
            {
                BackgroundSource = AcrylicBackgroundSource.Backdrop,
                TintColor = baseColor,
                TintOpacity = 0.15,
                FallbackColor = Color.FromArgb(180, baseColor.R, baseColor.G, baseColor.B)
            };
        }

        private void DisplayLineContainer_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                ApplyReadingTypography(element);
            }
        }

        private void ApplyReadingTypography(DependencyObject root)
        {
            if (root == null || _viewModel == null) return;

            var fontFamily = new FontFamily(_viewModel.Font);
            var fontSize = _viewModel.FontSize;
            var showVerseNumbers = _viewModel.AppSettings?.ShowVerseNumbers ?? true;
            ApplyReadingTypographyRecursive(root, fontFamily, fontSize, showVerseNumbers);
        }

        private void ApplyReadingTypographyRecursive(DependencyObject node, FontFamily fontFamily, double fontSize, bool showVerseNumbers)
        {
            if (node is TextBlock textBlock)
            {
                textBlock.FontFamily = fontFamily;
                var tag = textBlock.Tag as string;
                switch (tag)
                {
                    case "heading":
                        textBlock.FontSize = fontSize * 1.2;
                        break;
                    case "versenum":
                        textBlock.FontSize = fontSize * 0.7;
                        textBlock.Visibility = showVerseNumbers ? Visibility.Visible : Visibility.Collapsed;
                        break;
                    case "footnote":
                        textBlock.FontSize = fontSize * 0.75;
                        break;
                    default:
                        textBlock.FontSize = fontSize;
                        break;
                }
            }

            var childCount = VisualTreeHelper.GetChildrenCount(node);
            for (int i = 0; i < childCount; i++)
            {
                ApplyReadingTypographyRecursive(VisualTreeHelper.GetChild(node, i), fontFamily, fontSize, showVerseNumbers);
            }
        }

        private void UpdateCurrentChapterFromScroll()
        {
            if (!_isLoaded || _isAwaitingChapterSelection || _viewModel.Chapters.Count == 0) return;

            try
            {
                var chapterAtAnchor = GetChapterAtReadingAnchor();
                if (chapterAtAnchor != null && _viewModel.CurrentChapter != chapterAtAnchor)
                {
                    _isUpdatingFromScroll = true;
                    try
                    {
                        _viewModel.UpdateCurrentChapterFromScroll(chapterAtAnchor);
                        SynchronizePickerSelection();
                    }
                    finally
                    {
                        _isUpdatingFromScroll = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating chapter from scroll: {ex.Message}");
            }
        }

        private Chapter GetChapterAtReadingAnchor()
        {
            if (_viewModel.Chapters.Count == 0)
            {
                return null;
            }

            if (BibleScrollViewer.VerticalOffset <= 1)
            {
                return _viewModel.Chapters[0];
            }

            var anchorY = ScrollAnchorY;
            Chapter closestBeforeAnchor = null;
            Chapter firstAfterAnchor = null;

            for (int i = 0; i < _viewModel.Chapters.Count; i++)
            {
                var element = ChaptersRepeater.TryGetElement(i);
                if (!(element is FrameworkElement frameworkElement))
                {
                    continue;
                }

                var transform = frameworkElement.TransformToVisual(BibleScrollViewer);
                var elementPosition = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
                var elementTop = elementPosition.Y;
                var elementBottom = elementTop + frameworkElement.ActualHeight;

                if (elementTop <= anchorY && elementBottom >= anchorY)
                {
                    return _viewModel.Chapters[i];
                }

                if (elementTop <= anchorY)
                {
                    closestBeforeAnchor = _viewModel.Chapters[i];
                    continue;
                }

                firstAfterAnchor = _viewModel.Chapters[i];
                break;
            }

            return closestBeforeAnchor ?? firstAfterAnchor ?? _viewModel.Chapters[0];
        }

        private void BibleScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (!_isLoaded) return;

            _chooserTranslate.Y = 0;

            if (!e.IsIntermediate)
            {
                if (DateTimeOffset.UtcNow < _ignoreScrollSyncUntil)
                {
                    return;
                }

                UpdateCurrentChapterFromScroll();
            }
        }

        private async void BibleBookChapterComboBoxes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded || _isUpdatingFromScroll || _suppressComboHandling) return;

            if (sender == BibleBookComboBox)
            {
                if (!(BibleBookComboBox.SelectedItem is Book selectedBook))
                {
                    SynchronizePickerSelection();
                    return;
                }

                if (selectedBook == _viewModel.CurrentBook)
                {
                    _isAwaitingChapterSelection = false;
                    _viewModel.RestoreChapterPickerToCurrentBook();
                    SynchronizePickerSelection();
                    return;
                }

                await BeginPendingBookSelectionAsync(selectedBook);
                return;
            }

            if (!(BookChapterComboBox.SelectedItem is int selectedChapterIndex)) return;

            var targetBook = _isAwaitingChapterSelection && BibleBookComboBox.SelectedItem is Book pendingBook
                ? pendingBook
                : _viewModel.CurrentBook;

            _isAwaitingChapterSelection = false;
            await CommitNavigationSelectionAsync(targetBook, selectedChapterIndex, saveToHistory: true);
        }

        private void BookChapterComboBox_DropDownClosed(object sender, object e)
        {
            if (!_isLoaded || !_isAwaitingChapterSelection) return;

            _isAwaitingChapterSelection = false;
            _viewModel.RestoreChapterPickerToCurrentBook();
            SynchronizePickerSelection();
        }

        public async Task NavigateToFromHistoryAsync(string bookTitle, int chapter, string bookKey = null)
        {
            if (!_isLoaded)
            {
                _pendingHistoryBook = bookTitle;
                _pendingHistoryChapter = chapter;
                _pendingHistoryBookKey = bookKey;
                return;
            }

            _isAwaitingChapterSelection = false;
            SuppressScrollSyncFor(TimeSpan.FromSeconds(2));
            BookChapterComboBox.IsDropDownOpen = false;
            await _viewModel.UpdateNavigationSettingsAsync(bookTitle, chapter, bookKey);
            SynchronizePickerSelection();
            await PositionReaderAsync(waitForLayout: true);
        }

        public async Task NavigateToFromSearchAsync(SearchNavigationParameter searchParam)
        {
            if (!_isLoaded)
            {
                _pendingSearchParam = searchParam;
                return;
            }

            ClearVerseHighlight();
            _isAwaitingChapterSelection = false;
            SuppressScrollSyncFor(TimeSpan.FromSeconds(2));
            BookChapterComboBox.IsDropDownOpen = false;
            await _viewModel.UpdateNavigationSettingsAsync(searchParam.BookTitle, searchParam.ChapterIndex, searchParam.BookKey);
            SynchronizePickerSelection();
            await PositionReaderAsync(waitForLayout: true);
            await HighlightVerseAsync(searchParam.ChapterIndex, searchParam.VerseNumber);
        }

        private async Task HighlightVerseAsync(int chapterIndex, int verseNumber)
        {
            // Allow layout to settle before walking the visual tree
            await Task.Delay(200);
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ClearVerseHighlight();
                var verseElement = FindVerseElement(chapterIndex, verseNumber);
                if (verseElement != null)
                {
                    var isDark = ActualTheme == ElementTheme.Dark ||
                                 (ActualTheme == ElementTheme.Default && Application.Current.RequestedTheme == ApplicationTheme.Dark);
                    var highlightColor = isDark
                        ? Color.FromArgb(80, 255, 200, 50)
                        : Color.FromArgb(80, 255, 230, 100);

                    verseElement.Background = new SolidColorBrush(highlightColor);
                    _highlightedElement = verseElement;

                    ScrollVerseIntoView(verseElement);
                }
            });
        }

        private void ClearVerseHighlight()
        {
            if (_highlightedElement != null)
            {
                if (_highlightedElement is Panel panel)
                {
                    panel.Background = null;
                }
                else if (_highlightedElement is Border border)
                {
                    border.Background = null;
                }
                _highlightedElement = null;
            }
        }

        private StackPanel FindVerseElement(int chapterIndex, int verseNumber)
        {
            if (_viewModel?.Chapters == null) return null;

            var chapterIdx = -1;
            for (int i = 0; i < _viewModel.Chapters.Count; i++)
            {
                if (_viewModel.Chapters[i].Index == chapterIndex)
                {
                    chapterIdx = i;
                    break;
                }
            }

            if (chapterIdx < 0) return null;

            var chapterElement = ChaptersRepeater.TryGetElement(chapterIdx) as FrameworkElement;
            if (chapterElement == null) return null;

            // Walk the visual tree to find the verse's StackPanel container
            return FindVerseInTree(chapterElement, verseNumber);
        }

        private static StackPanel FindVerseInTree(DependencyObject root, int verseNumber)
        {
            if (root == null) return null;

            // Look for a StackPanel with a child Grid that contains a TextBlock with the verse number
            if (root is StackPanel sp)
            {
                var childCount = VisualTreeHelper.GetChildrenCount(sp);
                for (int i = 0; i < childCount; i++)
                {
                    var child = VisualTreeHelper.GetChild(sp, i);
                    if (child is Grid grid)
                    {
                        var verseNumBlock = FindDescendantWithTag<TextBlock>(grid, "versenum");
                        if (verseNumBlock != null && verseNumBlock.Text == verseNumber.ToString())
                        {
                            return sp;
                        }
                    }
                }
            }

            var count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var result = FindVerseInTree(VisualTreeHelper.GetChild(root, i), verseNumber);
                if (result != null) return result;
            }

            return null;
        }

        private static T FindDescendantWithTag<T>(DependencyObject root, string tag) where T : FrameworkElement
        {
            if (root == null) return null;
            if (root is T typed && (typed.Tag as string) == tag) return typed;

            var childCount = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < childCount; i++)
            {
                var result = FindDescendantWithTag<T>(VisualTreeHelper.GetChild(root, i), tag);
                if (result != null) return result;
            }

            return null;
        }

        private void ScrollVerseIntoView(FrameworkElement element)
        {
            try
            {
                var transform = element.TransformToVisual(BibleScrollViewer);
                var position = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
                var targetY = BibleScrollViewer.VerticalOffset + position.Y - (BibleScrollViewer.ViewportHeight / 3);
                targetY = Math.Max(0, targetY);
                BibleScrollViewer.ChangeView(null, targetY, null, false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error scrolling verse into view: {ex.Message}");
            }
        }

        private async Task BeginPendingBookSelectionAsync(Book selectedBook)
        {
            _isAwaitingChapterSelection = true;
            _suppressComboHandling = true;
            try
            {
                await _viewModel.PrepareChapterPickerAsync(selectedBook);
                BookChapterComboBox.ItemsSource = _viewModel.ChapterIndices;
                BookChapterComboBox.SelectedItem = null;
            }
            finally
            {
                _suppressComboHandling = false;
            }

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                BookChapterComboBox.IsDropDownOpen = true;
            });

            await ResetChapterPickerScrollAsync();
        }

        private async Task CommitNavigationSelectionAsync(Book book, int chapterIndex, bool saveToHistory)
        {
            if (book == null) return;

            SuppressScrollSyncFor(TimeSpan.FromSeconds(2));
            BookChapterComboBox.IsDropDownOpen = false;
            await _viewModel.SetCurrentLocationAsync(book, chapterIndex);
            SynchronizePickerSelection();
            await PositionReaderAsync(waitForLayout: true);

            if (saveToHistory)
            {
                SaveCurrentSelectionToHistory();
            }
        }

        private async Task PositionReaderAsync(bool waitForLayout)
        {
            SuppressScrollSyncFor(TimeSpan.FromSeconds(2));

            if (waitForLayout)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => BibleScrollViewer.UpdateLayout());
                await Task.Delay(LayoutSettleDelayMs);
            }

            ScrollToCurrentChapter();
        }

        private void SynchronizePickerSelection()
        {
            _suppressComboHandling = true;
            try
            {
                BibleBookComboBox.SelectedItem = _viewModel.CurrentBook;
                BookChapterComboBox.ItemsSource = _viewModel.ChapterIndices;
                BookChapterComboBox.SelectedItem = _viewModel.SelectedChapterIndex;
            }
            finally
            {
                _suppressComboHandling = false;
            }
        }

        private void ClearPendingHistory()
        {
            _pendingHistoryBook = null;
            _pendingHistoryChapter = 0;
            _pendingHistoryBookKey = null;
        }

        private void SaveCurrentSelectionToHistory()
        {
            try
            {
                var settingsService = App.Services.GetRequiredService<Core.Interfaces.ISettingsService>();
                var history = settingsService.GetNavigationHistory() ?? new List<NavigationHistoryItem>();
                var currentBookTitle = _viewModel.CurrentBook?.Title ?? _viewModel.Bible?.Books?.FirstOrDefault()?.Title ?? string.Empty;
                var currentBookKey = Core.Dictionaries.EBookToLocation.EBookTitleToEBook.TryGetValue(currentBookTitle, out var bookEnum)
                    ? bookEnum.ToString()
                    : null;
                var currentChapter = _viewModel.SelectedChapterIndex;

                if (history.Count == 0
                    || history[history.Count - 1].BookTitle != currentBookTitle
                    || history[history.Count - 1].Chapter != currentChapter
                    || history[history.Count - 1].BookKey != currentBookKey)
                {
                    history.Add(new NavigationHistoryItem
                    {
                        BookTitle = currentBookTitle,
                        Chapter = currentChapter,
                        BookKey = currentBookKey
                    });

                    if (history.Count > 10) history.RemoveAt(0);
                    settingsService.SaveNavigationHistory(history);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving navigation history: {ex.Message}");
            }
        }

        private async Task ResetChapterPickerScrollAsync()
        {
            for (int attempt = 0; attempt < ComboResetMaxAttempts; attempt++)
            {
                await Task.Delay(ComboResetRetryDelayMs);

                var scrollReset = false;
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    foreach (var popup in VisualTreeHelper.GetOpenPopups(Window.Current))
                    {
                        var popupScrollViewer = FindDescendant<ScrollViewer>(popup.Child);
                        if (popupScrollViewer == null) continue;

                        popupScrollViewer.ChangeView(null, 0, null, true);
                        scrollReset = true;
                        break;
                    }
                });

                if (scrollReset)
                {
                    break;
                }
            }
        }

        private void SuppressScrollSyncFor(TimeSpan duration)
        {
            var suppressionEnd = DateTimeOffset.UtcNow.Add(duration);
            if (suppressionEnd > _ignoreScrollSyncUntil)
            {
                _ignoreScrollSyncUntil = suppressionEnd;
            }
        }

        private static T FindDescendant<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null) return null;
            if (root is T typedRoot) return typedRoot;

            var childCount = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < childCount; i++)
            {
                var result = FindDescendant<T>(VisualTreeHelper.GetChild(root, i));
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
