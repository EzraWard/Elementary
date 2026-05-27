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
        private const double ChapterTopOffset = 124d;
        private const int LayoutSettleDelayMs = 100;
        private const int NavigationSpinnerDelayMs = 120;
        private const int NavigationScrollSyncSuppressionMs = 500;
        private const int IntermediateScrollSyncThrottleMs = 100;
        private const int ScrollStatePersistenceDelayMs = 250;
        private const int ComboResetMaxAttempts = 5;
        private const int ComboResetRetryDelayMs = 20;
        private const int ChapterElementWaitMaxAttempts = 12;
        private const int ChapterElementWaitDelayMs = 25;

        private readonly BiblePageViewModel _viewModel;
        private ScrollViewer _readerScrollViewer;
        private bool _isLoaded;
        private bool _isInitializing;
        private bool _isUpdatingFromScroll;
        private bool _isProgrammaticNavigation;
        private bool _isAwaitingChapterSelection;
        private bool _suppressComboHandling;
        private DateTimeOffset _ignoreScrollSyncUntil = DateTimeOffset.MinValue;
        private readonly TranslateTransform _chooserTranslate = new TranslateTransform();
        private readonly Dictionary<Chapter, FrameworkElement> _chapterElements = new Dictionary<Chapter, FrameworkElement>();
        private string _pendingHistoryBook;
        private int _pendingHistoryChapter;
        private string _pendingHistoryBookKey;
        private SearchNavigationParameter _pendingSearchParam;
        private Panel _highlightedElement;
        private int _navigationVisualStateVersion;
        private DateTimeOffset _lastIntermediateScrollSyncAt = DateTimeOffset.MinValue;
        private bool _isProcessingScrollSync;
        private bool _hasPendingScrollSync;
        private int _scrollLocationPersistenceVersion;

        public BiblePage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Disabled;

            _viewModel = new BiblePageViewModel();
            DataContext = _viewModel;

            Loaded += BiblePage_Loaded;
            ActualThemeChanged += BiblePage_ActualThemeChanged;
            ChooserBorder.RenderTransform = _chooserTranslate;
            SetReaderVisualState(showContent: false, showSpinner: true);
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

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            ClearVerseHighlight();
            _scrollLocationPersistenceVersion++;
            if (_isLoaded)
            {
                _viewModel.PersistCurrentLocation();
            }
        }

        private async void BiblePage_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isInitializing || _isLoaded) return;

            _isInitializing = true;
            try
            {
                _viewModel.IsLoaded = false;
                SetPickerInteractionEnabled(false);
                SetReaderVisualState(showContent: false, showSpinner: true);

                await _viewModel.Initialize();
                ResetChapterElementTracking();
                _chooserTranslate.Y = 0;
                SetupTopFadeGradient();
                await EnsureReaderScrollViewerAsync();

                SearchNavigationParameter pendingSearch = null;
                if (_pendingSearchParam != null)
                {
                    pendingSearch = _pendingSearchParam;
                    _pendingSearchParam = null;

                    var pendingWindowChanged = await _viewModel.UpdateNavigationSettingsAsync(pendingSearch.BookTitle, pendingSearch.ChapterIndex, pendingSearch.BookKey);
                    if (pendingWindowChanged)
                    {
                        ResetChapterElementTracking();
                    }
                }
                else if (_pendingHistoryBook != null)
                {
                    var pendingBook = _pendingHistoryBook;
                    var pendingChapter = _pendingHistoryChapter;
                    var pendingBookKey = _pendingHistoryBookKey;
                    ClearPendingHistory();

                    var pendingWindowChanged = await _viewModel.UpdateNavigationSettingsAsync(pendingBook, pendingChapter, pendingBookKey);
                    if (pendingWindowChanged)
                    {
                        ResetChapterElementTracking();
                    }
                }

                SynchronizePickerSelection();
                await PositionReaderAsync(waitForLayout: true);

                _viewModel.IsLoaded = true;
                _isLoaded = true;
                SetReaderVisualState(showContent: true, showSpinner: false);
                SetPickerInteractionEnabled(true);

                if (pendingSearch != null)
                {
                    await HighlightVerseAsync(pendingSearch.ChapterIndex, pendingSearch.VerseNumber);
                }
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private void BiblePage_ActualThemeChanged(FrameworkElement sender, object args)
        {
            SetupTopFadeGradient();
        }

        private void ApplyTopOffsetToFirstChapter()
        {
            try
            {
                var firstElement = GetChapterElement(0);
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
            var isDark = ActualTheme == ElementTheme.Dark ||
                         (ActualTheme == ElementTheme.Default && Application.Current.RequestedTheme == ApplicationTheme.Dark);
            if (Application.Current.Resources.TryGetValue("ApplicationPageBackgroundThemeBrush", out var res)
                && res is SolidColorBrush bgBrush)
            {
                baseColor = bgBrush.Color;
            }
            else
            {
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
                TintOpacity = isDark ? 0.30 : 0.20,
                FallbackColor = Color.FromArgb(isDark ? (byte)210 : (byte)225, baseColor.R, baseColor.G, baseColor.B)
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

        private async Task UpdateCurrentChapterFromScrollAsync()
        {
            if (!_isLoaded || _isAwaitingChapterSelection || _isProgrammaticNavigation || _viewModel.Chapters.Count == 0) return;

            try
            {
                var chapterAtAnchor = GetChapterAtReadingAnchor();
                if (chapterAtAnchor == null)
                {
                    return;
                }

                var currentChapterChanged = _viewModel.CurrentChapter != chapterAtAnchor;
                if (currentChapterChanged)
                {
                    _isUpdatingFromScroll = true;
                    try
                    {
                        _viewModel.UpdateCurrentChapterFromScroll(chapterAtAnchor);
                        SynchronizePickerSelection();
                        ScheduleCurrentLocationPersistence();
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

            if (_readerScrollViewer == null || _readerScrollViewer.VerticalOffset <= 1)
            {
                return _viewModel.Chapters[0];
            }

            var realizedChapters = GetRealizedChaptersOrderedByTop().OrderBy(item => item.Top).ToList();
            if (realizedChapters.Count == 0)
            {
                return _viewModel.CurrentChapter ?? _viewModel.Chapters[0];
            }

            var anchorY = ScrollAnchorY;
            Chapter closestBeforeAnchor = null;
            Chapter firstAfterAnchor = null;

            foreach (var realizedChapter in realizedChapters)
            {
                var frameworkElement = realizedChapter.Element;
                var elementTop = realizedChapter.Top;
                var elementBottom = elementTop + frameworkElement.ActualHeight;

                if (elementTop <= anchorY && elementBottom >= anchorY)
                {
                    return realizedChapter.Chapter;
                }

                if (elementTop <= anchorY)
                {
                    closestBeforeAnchor = realizedChapter.Chapter;
                    continue;
                }

                firstAfterAnchor = realizedChapter.Chapter;
                break;
            }

            return closestBeforeAnchor ?? firstAfterAnchor ?? _viewModel.Chapters[0];
        }

        private void BibleScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (!_isLoaded) return;

            _chooserTranslate.Y = 0;

            if (_isProgrammaticNavigation || DateTimeOffset.UtcNow < _ignoreScrollSyncUntil)
            {
                return;
            }

            if (e.IsIntermediate)
            {
                var now = DateTimeOffset.UtcNow;
                if ((now - _lastIntermediateScrollSyncAt).TotalMilliseconds < IntermediateScrollSyncThrottleMs)
                {
                    return;
                }

                _lastIntermediateScrollSyncAt = now;
            }

            QueueScrollSync();
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
            await ExecuteNavigationTransitionAsync(async () =>
            {
                SuppressScrollSyncFor(TimeSpan.FromMilliseconds(NavigationScrollSyncSuppressionMs));
                BookChapterComboBox.IsDropDownOpen = false;
                var windowChanged = await _viewModel.UpdateNavigationSettingsAsync(bookTitle, chapter, bookKey);
                if (windowChanged)
                {
                    ResetChapterElementTracking();
                }

                SynchronizePickerSelection();
                await PositionReaderAsync(waitForLayout: true);
            });
        }

        public async Task NavigateToFromSearchAsync(SearchNavigationParameter searchParam)
        {
            if (searchParam == null) return;

            if (!_isLoaded)
            {
                _pendingSearchParam = searchParam;
                return;
            }

            ClearVerseHighlight();
            _isAwaitingChapterSelection = false;
            await ExecuteNavigationTransitionAsync(async () =>
            {
                SuppressScrollSyncFor(TimeSpan.FromMilliseconds(NavigationScrollSyncSuppressionMs));
                BookChapterComboBox.IsDropDownOpen = false;
                var windowChanged = await _viewModel.UpdateNavigationSettingsAsync(searchParam.BookTitle, searchParam.ChapterIndex, searchParam.BookKey);
                if (windowChanged)
                {
                    ResetChapterElementTracking();
                }

                SynchronizePickerSelection();
                await PositionReaderAsync(waitForLayout: true);
                await HighlightVerseAsync(searchParam.ChapterIndex, searchParam.VerseNumber);
            });
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

            await ExecuteNavigationTransitionAsync(async () =>
            {
                SuppressScrollSyncFor(TimeSpan.FromMilliseconds(NavigationScrollSyncSuppressionMs));
                BookChapterComboBox.IsDropDownOpen = false;
                var windowChanged = await _viewModel.SetCurrentLocationAsync(book, chapterIndex);
                if (windowChanged)
                {
                    ResetChapterElementTracking();
                }

                SynchronizePickerSelection();
                await PositionReaderAsync(waitForLayout: true);

                if (saveToHistory)
                {
                    SaveCurrentSelectionToHistory();
                }
            });
        }

        private async Task PositionReaderAsync(bool waitForLayout)
        {
            SuppressScrollSyncFor(TimeSpan.FromMilliseconds(NavigationScrollSyncSuppressionMs));

            if (waitForLayout)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => BibleChaptersListView.UpdateLayout());
                await Task.Delay(LayoutSettleDelayMs);
            }

            await EnsureReaderScrollViewerAsync();
            await ScrollToCurrentChapterAsync();
        }

        private FrameworkElement GetChapterElement(int chapterIndex)
        {
            var chapter = _viewModel?.Chapters?.ElementAtOrDefault(chapterIndex);
            if (chapter == null)
            {
                return null;
            }

            if (BibleChaptersListView.ContainerFromIndex(chapterIndex) is ListViewItem container)
            {
                if (container.ContentTemplateRoot is FrameworkElement contentTemplateRoot)
                {
                    return contentTemplateRoot;
                }

                return FindDescendant<FrameworkElement>(container);
            }

            return _chapterElements.TryGetValue(chapter, out var element) ? element : null;
        }

        private async Task EnsureReaderScrollViewerAsync()
        {
            if (_readerScrollViewer != null)
            {
                return;
            }

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => BibleChaptersListView.UpdateLayout());
            _readerScrollViewer = FindDescendant<ScrollViewer>(BibleChaptersListView);
            if (_readerScrollViewer != null)
            {
                _readerScrollViewer.ViewChanged -= BibleScrollViewer_ViewChanged;
                _readerScrollViewer.ViewChanged += BibleScrollViewer_ViewChanged;
            }
        }

        private async Task ScrollToCurrentChapterAsync()
        {
            if (_viewModel?.Chapters == null || _viewModel.Chapters.Count == 0 || _viewModel.CurrentChapter == null)
            {
                return;
            }

            await EnsureReaderScrollViewerAsync();
            if (_readerScrollViewer == null)
            {
                return;
            }

            BibleChaptersListView.ScrollIntoView(_viewModel.CurrentChapter, ScrollIntoViewAlignment.Leading);
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => BibleChaptersListView.UpdateLayout());
            await Task.Delay(LayoutSettleDelayMs);

            var currentChapterIndex = _viewModel.Chapters.IndexOf(_viewModel.CurrentChapter);
            if (currentChapterIndex < 0)
            {
                return;
            }

            var element = await WaitForChapterElementAsync(currentChapterIndex);
            if (element == null)
            {
                return;
            }

            var chapterTopInViewport = GetChapterTopInViewport(element);
            var adjustedOffset = Math.Max(0, _readerScrollViewer.VerticalOffset + chapterTopInViewport - ChapterTopOffset);
            _readerScrollViewer.ChangeView(null, adjustedOffset, null, true);
            ApplyTopOffsetToFirstChapter();
        }

        private async Task<FrameworkElement> WaitForChapterElementAsync(int chapterIndex)
        {
            for (int attempt = 0; attempt < ChapterElementWaitMaxAttempts; attempt++)
            {
                var element = GetChapterElement(chapterIndex);
                if (element?.DataContext is Chapter chapter
                    && chapter.Index > 0
                    && chapter.DisplayLines != null
                    && chapter.DisplayLines.Count > 0)
                {
                    return element;
                }

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => BibleChaptersListView.UpdateLayout());
                await Task.Delay(ChapterElementWaitDelayMs);
            }

            return GetChapterElement(chapterIndex);
        }

        private async Task HighlightVerseAsync(int chapterIndex, int verseNumber)
        {
            if (verseNumber <= 0)
            {
                return;
            }

            var chapterListIndex = FindChapterListIndex(chapterIndex);
            if (chapterListIndex < 0)
            {
                return;
            }

            await EnsureReaderScrollViewerAsync();
            if (_readerScrollViewer == null)
            {
                return;
            }

            BibleChaptersListView.ScrollIntoView(_viewModel.Chapters[chapterListIndex], ScrollIntoViewAlignment.Leading);
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => BibleChaptersListView.UpdateLayout());
            await Task.Delay(LayoutSettleDelayMs);

            var verseElement = await WaitForVerseElementAsync(chapterListIndex, verseNumber);
            if (verseElement == null)
            {
                return;
            }

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ClearVerseHighlight();

                var isDark = ActualTheme == ElementTheme.Dark ||
                             (ActualTheme == ElementTheme.Default && Application.Current.RequestedTheme == ApplicationTheme.Dark);
                var highlightColor = isDark
                    ? Color.FromArgb(80, 255, 200, 50)
                    : Color.FromArgb(80, 255, 230, 100);

                verseElement.Background = new SolidColorBrush(highlightColor);
                _highlightedElement = verseElement;
                ScrollElementIntoView(verseElement);
            });
        }

        private async Task<Panel> WaitForVerseElementAsync(int chapterListIndex, int verseNumber)
        {
            for (int attempt = 0; attempt < ChapterElementWaitMaxAttempts; attempt++)
            {
                var chapterElement = await WaitForChapterElementAsync(chapterListIndex);
                var verseElement = FindVerseElement(chapterElement, verseNumber);
                if (verseElement != null)
                {
                    return verseElement;
                }

                BibleChaptersListView.ScrollIntoView(_viewModel.Chapters[chapterListIndex], ScrollIntoViewAlignment.Leading);
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => BibleChaptersListView.UpdateLayout());
                await Task.Delay(ChapterElementWaitDelayMs);
            }

            return null;
        }

        private int FindChapterListIndex(int chapterIndex)
        {
            if (_viewModel?.Chapters == null)
            {
                return -1;
            }

            for (int i = 0; i < _viewModel.Chapters.Count; i++)
            {
                if (_viewModel.Chapters[i].Index == chapterIndex)
                {
                    return i;
                }
            }

            return -1;
        }

        private static Panel FindVerseElement(DependencyObject root, int verseNumber)
        {
            if (root == null)
            {
                return null;
            }

            if (root is Panel panel
                && panel.DataContext is ChapterDisplayLine line
                && line.Type == ChapterDisplayLineType.Verse
                && line.VerseNumber == verseNumber)
            {
                return panel;
            }

            var childCount = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < childCount; i++)
            {
                var result = FindVerseElement(VisualTreeHelper.GetChild(root, i), verseNumber);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private void ScrollElementIntoView(FrameworkElement element)
        {
            if (element == null || _readerScrollViewer == null)
            {
                return;
            }

            var transform = element.TransformToVisual(_readerScrollViewer);
            var position = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
            var targetOffset = Math.Max(0, _readerScrollViewer.VerticalOffset + position.Y - (_readerScrollViewer.ViewportHeight / 3));
            _readerScrollViewer.ChangeView(null, targetOffset, null, false);
        }

        private void ClearVerseHighlight()
        {
            if (_highlightedElement == null)
            {
                return;
            }

            _highlightedElement.Background = null;
            _highlightedElement = null;
        }

        private void QueueScrollSync()
        {
            _hasPendingScrollSync = true;
            if (_isProcessingScrollSync)
            {
                return;
            }

            _ = ProcessPendingScrollSyncAsync();
        }

        private async Task ProcessPendingScrollSyncAsync()
        {
            if (_isProcessingScrollSync)
            {
                return;
            }

            _isProcessingScrollSync = true;
            try
            {
                while (_hasPendingScrollSync)
                {
                    _hasPendingScrollSync = false;
                    await UpdateCurrentChapterFromScrollAsync();
                }
            }
            finally
            {
                _isProcessingScrollSync = false;
                if (_hasPendingScrollSync)
                {
                    _ = ProcessPendingScrollSyncAsync();
                }
            }
        }

        private void ScheduleCurrentLocationPersistence()
        {
            var persistenceVersion = ++_scrollLocationPersistenceVersion;
            _ = PersistCurrentLocationAfterDelayAsync(persistenceVersion);
        }

        private async Task PersistCurrentLocationAfterDelayAsync(int persistenceVersion)
        {
            await Task.Delay(ScrollStatePersistenceDelayMs);
            if (persistenceVersion != _scrollLocationPersistenceVersion || !_isLoaded)
            {
                return;
            }

            await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                if (persistenceVersion == _scrollLocationPersistenceVersion && _isLoaded)
                {
                    _viewModel.PersistCurrentLocation();
                }
            });
        }

        private double GetChapterTopInViewport(FrameworkElement chapterElement)
        {
            if (chapterElement == null)
            {
                return 0d;
            }

            var transform = chapterElement.TransformToVisual(_readerScrollViewer);
            var chapterPosition = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
            return chapterPosition.Y;
        }

        private IEnumerable<(Chapter Chapter, FrameworkElement Element, double Top)> GetRealizedChaptersOrderedByTop()
        {
            if (_viewModel?.Chapters == null)
            {
                yield break;
            }

            for (int i = 0; i < _viewModel.Chapters.Count; i++)
            {
                var chapter = _viewModel.Chapters[i];
                var element = GetChapterElement(i);
                if (chapter == null || element == null || element.ActualHeight <= 0)
                {
                    continue;
                }

                yield return (chapter, element, GetChapterTopInViewport(element));
            }
        }

        private void ChapterItemGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Chapter chapter)
            {
                _chapterElements[chapter] = element;
            }
        }

        private void ChapterItemGrid_Unloaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement element) || !(element.DataContext is Chapter chapter))
            {
                return;
            }

            if (_chapterElements.TryGetValue(chapter, out var loadedElement) && ReferenceEquals(loadedElement, element))
            {
                _chapterElements.Remove(chapter);
            }
        }

        private void ResetChapterElementTracking()
        {
            _chapterElements.Clear();
        }

        private void SynchronizePickerSelection()
        {
            _suppressComboHandling = true;
            try
            {
                if (!ReferenceEquals(BibleBookComboBox.SelectedItem, _viewModel.CurrentBook))
                {
                    BibleBookComboBox.SelectedItem = _viewModel.CurrentBook;
                }

                if (!ReferenceEquals(BookChapterComboBox.ItemsSource, _viewModel.ChapterIndices))
                {
                    BookChapterComboBox.ItemsSource = _viewModel.ChapterIndices;
                }

                if (!(BookChapterComboBox.SelectedItem is int selectedChapterIndex) || selectedChapterIndex != _viewModel.SelectedChapterIndex)
                {
                    BookChapterComboBox.SelectedItem = _viewModel.SelectedChapterIndex;
                }
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

        private async Task ExecuteNavigationTransitionAsync(Func<Task> navigationAction)
        {
            var navigationVersion = ++_navigationVisualStateVersion;
            _isProgrammaticNavigation = true;
            SetPickerInteractionEnabled(false);
            SetReaderVisualState(showContent: false, showSpinner: false);
            _ = ShowNavigationSpinnerIfStillPendingAsync(navigationVersion);

            try
            {
                await navigationAction();
                SynchronizePickerSelection();
            }
            finally
            {
                await Task.Delay(LayoutSettleDelayMs);
                _isProgrammaticNavigation = false;
                if (navigationVersion == _navigationVisualStateVersion)
                {
                    SetReaderVisualState(showContent: true, showSpinner: false);
                    SetPickerInteractionEnabled(true);
                }
            }
        }

        private async Task ShowNavigationSpinnerIfStillPendingAsync(int navigationVersion)
        {
            await Task.Delay(NavigationSpinnerDelayMs);
            if (navigationVersion != _navigationVisualStateVersion)
            {
                return;
            }

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (navigationVersion == _navigationVisualStateVersion)
                {
                    SetReaderVisualState(showContent: false, showSpinner: true);
                }
            });
        }

        private void SetReaderVisualState(bool showContent, bool showSpinner)
        {
            BibleChaptersListView.Opacity = showContent ? 1 : 0;
            BiblePageProgressRing.Visibility = showSpinner ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SetPickerInteractionEnabled(bool isEnabled)
        {
            BibleBookComboBox.IsEnabled = isEnabled;
            BookChapterComboBox.IsEnabled = isEnabled;
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
