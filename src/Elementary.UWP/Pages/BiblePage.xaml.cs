using Elementary.Core.Interfaces;
using Elementary.Core.Models;
using Elementary.Core.Services;
using Elementary.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Windows.ApplicationModel;
using Windows.System.Display;
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
        // End the preceding chapter inside the 90px top fade. The heading supplies another
        // 24px top margin, placing "Chapter N" just below the overlay and fully readable.
        private const double ChapterTopOffset = 72d;
        private const double ScrollAnchorY = ChapterTopOffset + 8d;
        private const int LayoutSettleDelayMs = 100;
        private const int NavigationSpinnerDelayMs = 120;
        private const int NavigationScrollSyncSuppressionMs = 500;
        private const int IntermediateScrollSyncThrottleMs = 100;
        private const int ScrollStatePersistenceDelayMs = 250;
        private const int ComboResetMaxAttempts = 5;
        private const int ComboResetRetryDelayMs = 20;
        private const int ChapterElementWaitMaxAttempts = 12;
        private const int ChapterElementWaitDelayMs = 25;
        private const int ReaderPositionStabilizationMaxAttempts = 8;
        private const int ReaderPositionRequiredStablePasses = 2;
        private const double ReaderPositionTolerance = 1d;
        private const double InfiniteScrollEdgeThreshold = 900d;

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
        private readonly Dictionary<BibleReaderItem, FrameworkElement> _chapterElements = new Dictionary<BibleReaderItem, FrameworkElement>();
        private string _pendingHistoryBook;
        private int _pendingHistoryChapter;
        private string _pendingHistoryBookKey;
        private SearchNavigationParameter _pendingSearchParam;
        private Panel _highlightedElement;
        private TaskCompletionSource<bool> _pendingSearchNavigationCompletionSource;
        private int _navigationVisualStateVersion;
        private DateTimeOffset _lastIntermediateScrollSyncAt = DateTimeOffset.MinValue;
        private bool _isProcessingScrollSync;
        private bool _hasPendingScrollSync;
        private bool _isUpdatingReaderWindow;
        private int _scrollLocationPersistenceVersion;
        private readonly DispatcherTimer _readingSessionTimer;
        private readonly DisplayRequest _displayRequest = new DisplayRequest();
        private DateTimeOffset? _readingSessionStartedAt;
        private bool _isWindowVisible = true;
        private bool _isWindowActive = true;
        private bool _isReaderPageActive;
        private bool _isReaderObscured;
        private bool _isApplicationSuspended;
        private bool _areReadingLifecycleHandlersAttached;
        private bool _isDisplayRequestActive;

        public BiblePage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Required;

            _viewModel = new BiblePageViewModel();
            DataContext = _viewModel;

            Loaded += BiblePage_Loaded;
            Unloaded += BiblePage_Unloaded;
            ActualThemeChanged += BiblePage_ActualThemeChanged;
            ChooserBorder.RenderTransform = _chooserTranslate;
            SetReaderVisualState(showContent: false, showSpinner: true);

            _readingSessionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _readingSessionTimer.Tick += ReadingSessionTimer_Tick;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _isReaderPageActive = true;

            if (e.Parameter is SearchNavigationParameter searchParam)
            {
                if (!_isLoaded)
                {
                    _pendingSearchParam = searchParam;
                    EnsurePendingSearchNavigationCompletionSource();
                    return;
                }

                _ = NavigateToFromSearchAsync(searchParam);
                return;
            }

            if (e.Parameter is NavigationHistoryItem historyItem)
            {
                if (!_isLoaded)
                {
                    _pendingHistoryBook = historyItem.BookTitle;
                    _pendingHistoryChapter = historyItem.Chapter;
                    _pendingHistoryBookKey = historyItem.BookKey;
                    return;
                }

                _ = NavigateToFromHistoryAsync(historyItem.BookTitle, historyItem.Chapter, historyItem.BookKey);
                return;
            }

            if (_isLoaded)
            {
                StartReadingSession();
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            _isReaderPageActive = false;
            UpdateReadingSessionState();
            ClearVerseHighlight();
            _scrollLocationPersistenceVersion++;
            if (_isLoaded)
            {
                _viewModel.PersistCurrentLocation();
            }
        }

        private async void BiblePage_Loaded(object sender, RoutedEventArgs e)
        {
            AttachReadingLifecycleHandlers();

            // This page is navigation-cached. A theme can change while Settings is visible,
            // when ActualThemeChanged is not delivered to the detached page.
            SetupTopFadeGradient();

            if (_isInitializing) return;

            if (_isLoaded)
            {
                var translationChanged = _viewModel.RefreshSettingsAndDetectTranslationChange();
                if (!translationChanged)
                {
                    ApplyReadingTypography(BibleChaptersListView);
                    UpdateReadingSessionState();
                    return;
                }

                // The cached page owns the Bible and its realized reader items. Reinitialize the
                // stream when the persisted translation changes so the new text appears now.
                _isLoaded = false;
            }

            _isInitializing = true;
            SearchNavigationParameter pendingSearch = null;
            try
            {
                _viewModel.IsLoaded = false;
                SetPickerInteractionEnabled(false);
                SetReaderVisualState(showContent: false, showSpinner: true);

                await _viewModel.Initialize();
                ResetChapterElementTracking();
                _chooserTranslate.Y = 0;
                await EnsureReaderScrollViewerAsync();

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
                UpdateNavigationHistory(departedLocation: null);
                SetReaderVisualState(showContent: true, showSpinner: false);
                SetPickerInteractionEnabled(true);
                StartReadingSession();

                if (pendingSearch != null)
                {
                    await HighlightVerseAsync(pendingSearch.ChapterIndex, pendingSearch.VerseNumber);
                    CompletePendingSearchNavigation();
                }
            }
            finally
            {
                if (pendingSearch != null)
                {
                    CompletePendingSearchNavigation();
                }

                _isInitializing = false;
            }
        }

        private void BiblePage_ActualThemeChanged(FrameworkElement sender, object args)
        {
            SetupTopFadeGradient();
        }

        private void BiblePage_Unloaded(object sender, RoutedEventArgs e)
        {
            DetachReadingLifecycleHandlers();
            StopReadingSession();
        }

        public void SetReadingObscured(bool isObscured)
        {
            if (_isReaderObscured == isObscured)
            {
                return;
            }

            _isReaderObscured = isObscured;
            UpdateReadingSessionState();
        }

        private void ApplyTopOffsetToFirstChapter()
        {
            try
            {
                var firstChapterItem = _viewModel?.ReaderItems?.FirstOrDefault(item => item.IsChapter);
                var firstElement = GetChapterElement(firstChapterItem);
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
            var isDark = ActualTheme == ElementTheme.Dark ||
                         (ActualTheme == ElementTheme.Default && Application.Current.RequestedTheme == ApplicationTheme.Dark);
            // A theme resource resolved directly from Application.Current.Resources keeps the
            // application's startup theme when RequestedTheme changes on the window content.
            // Use the page's resolved theme so the custom acrylic and gradient transition too.
            var baseColor = isDark
                ? Color.FromArgb(255, 32, 32, 32)
                : Color.FromArgb(255, 243, 243, 243);

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
                    case "bookheading":
                        textBlock.FontSize = fontSize * 1.9;
                        break;
                    case "chapterheading":
                        textBlock.FontSize = fontSize * 1.55;
                        break;
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
                    case "body":
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
            if (!_isLoaded || _isAwaitingChapterSelection || _isProgrammaticNavigation || _viewModel.ReaderItems.Count == 0) return;

            try
            {
                var readerItemAtAnchor = GetChapterAtReadingAnchor();
                if (readerItemAtAnchor == null)
                {
                    return;
                }

                var currentChapterChanged = _viewModel.CurrentChapter != readerItemAtAnchor.Chapter
                    || _viewModel.CurrentBook != readerItemAtAnchor.Book;
                if (currentChapterChanged)
                {
                    var departedLocation = CreateNavigationHistoryItem(_viewModel.CurrentBook, _viewModel.CurrentChapter);
                    _isUpdatingFromScroll = true;
                    try
                    {
                        _viewModel.UpdateCurrentChapterFromScroll(readerItemAtAnchor);
                        UpdateNavigationHistory(departedLocation);
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

        private BibleReaderItem GetChapterAtReadingAnchor()
        {
            if (_viewModel.ReaderItems.Count == 0)
            {
                return null;
            }

            if (_readerScrollViewer == null || _readerScrollViewer.VerticalOffset <= 1)
            {
                return _viewModel.ReaderItems.FirstOrDefault(item => item.IsChapter);
            }

            var realizedChapters = GetRealizedChaptersOrderedByTop().OrderBy(item => item.Top).ToList();
            if (realizedChapters.Count == 0)
            {
                return _viewModel.GetReaderItemForCurrentChapter()
                       ?? _viewModel.ReaderItems.FirstOrDefault(item => item.IsChapter);
            }

            var anchorY = ScrollAnchorY;
            BibleReaderItem closestBeforeAnchor = null;
            BibleReaderItem firstAfterAnchor = null;

            foreach (var realizedChapter in realizedChapters)
            {
                var frameworkElement = realizedChapter.Element;
                var elementTop = realizedChapter.Top;
                var elementBottom = elementTop + frameworkElement.ActualHeight;

                if (elementTop <= anchorY && elementBottom >= anchorY)
                {
                    return realizedChapter.ReaderItem;
                }

                if (elementTop <= anchorY)
                {
                    closestBeforeAnchor = realizedChapter.ReaderItem;
                    continue;
                }

                firstAfterAnchor = realizedChapter.ReaderItem;
                break;
            }

            return closestBeforeAnchor ?? firstAfterAnchor ?? _viewModel.ReaderItems.FirstOrDefault(item => item.IsChapter);
        }

        private void BibleScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (!_isLoaded) return;

            _chooserTranslate.Y = 0;

            if (_isProgrammaticNavigation || _isUpdatingReaderWindow || DateTimeOffset.UtcNow < _ignoreScrollSyncUntil)
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

            _ = EnsureReaderWindowForScrollAsync();
            QueueScrollSync();
        }

        private async Task EnsureReaderWindowForScrollAsync()
        {
            if (_isUpdatingReaderWindow || _readerScrollViewer == null || _viewModel?.ReaderItems == null)
            {
                return;
            }

            _isUpdatingReaderWindow = true;
            try
            {
                if (_readerScrollViewer.VerticalOffset < InfiniteScrollEdgeThreshold)
                {
                    await PrependPreviousBookPreservingPositionAsync();
                }

                var distanceFromBottom = _readerScrollViewer.ScrollableHeight - _readerScrollViewer.VerticalOffset;
                if (distanceFromBottom < InfiniteScrollEdgeThreshold)
                {
                    await AppendNextBookAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating reader window: {ex.Message}");
            }
            finally
            {
                _isUpdatingReaderWindow = false;
            }
        }

        private async Task AppendNextBookAsync()
        {
            if (await _viewModel.AppendNextBookAsync())
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => BibleChaptersListView.UpdateLayout());
            }
        }

        private async Task PrependPreviousBookPreservingPositionAsync()
        {
            var anchorItem = GetChapterAtReadingAnchor();
            var anchorElement = GetChapterElement(anchorItem);
            var anchorTopBefore = anchorElement != null ? GetChapterTopInViewport(anchorElement) : 0d;

            if (!await _viewModel.PrependPreviousBookAsync())
            {
                return;
            }

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => BibleChaptersListView.UpdateLayout());
            await Task.Delay(LayoutSettleDelayMs);

            var updatedAnchorElement = GetChapterElement(anchorItem);
            if (updatedAnchorElement == null)
            {
                return;
            }

            var anchorTopAfter = GetChapterTopInViewport(updatedAnchorElement);
            var adjustedOffset = Math.Max(0, _readerScrollViewer.VerticalOffset + anchorTopAfter - anchorTopBefore);
            SuppressScrollSyncFor(TimeSpan.FromMilliseconds(NavigationScrollSyncSuppressionMs));
            _readerScrollViewer.ChangeView(null, adjustedOffset, null, true);
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
                UpdateNavigationHistory(departedLocation: null);
            });
        }

        public async Task NavigateToFromSearchAsync(SearchNavigationParameter searchParam)
        {
            if (searchParam == null) return;

            if (!_isLoaded)
            {
                _pendingSearchParam = searchParam;
                var completionSource = EnsurePendingSearchNavigationCompletionSource();
                await completionSource.Task;
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
                UpdateNavigationHistory(departedLocation: null);
                await HighlightVerseAsync(searchParam.ChapterIndex, searchParam.VerseNumber);
            });
        }

        public void ClearSearchHighlight()
        {
            ClearVerseHighlight();
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

            var departedLocation = CreateNavigationHistoryItem(_viewModel.CurrentBook, _viewModel.CurrentChapter);
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
                    UpdateNavigationHistory(departedLocation);
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

        private FrameworkElement GetChapterElement(BibleReaderItem readerItem)
        {
            if (readerItem == null)
            {
                return null;
            }

            var itemIndex = _viewModel.ReaderItems.IndexOf(readerItem);
            if (itemIndex >= 0 && BibleChaptersListView.ContainerFromIndex(itemIndex) is ListViewItem container)
            {
                if (container.ContentTemplateRoot is FrameworkElement contentTemplateRoot)
                {
                    return contentTemplateRoot;
                }

                return FindDescendant<FrameworkElement>(container);
            }

            return _chapterElements.TryGetValue(readerItem, out var element) ? element : null;
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
            if (_viewModel?.ReaderItems == null || _viewModel.ReaderItems.Count == 0 || _viewModel.CurrentChapter == null)
            {
                return;
            }

            await EnsureReaderScrollViewerAsync();
            if (_readerScrollViewer == null)
            {
                return;
            }

            var currentChapterItem = _viewModel.GetReaderItemForCurrentChapter();
            if (currentChapterItem == null)
            {
                return;
            }

            var scrollTargetItem = ShouldPositionBookHeaderForCurrentChapter()
                ? _viewModel.GetReaderHeaderForCurrentBook() ?? currentChapterItem
                : currentChapterItem;

            BibleChaptersListView.ScrollIntoView(scrollTargetItem, ScrollIntoViewAlignment.Leading);
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => BibleChaptersListView.UpdateLayout());
            await Task.Delay(LayoutSettleDelayMs);

            var element = scrollTargetItem.IsChapter
                ? await WaitForChapterElementAsync(scrollTargetItem)
                : await WaitForReaderItemElementAsync(scrollTargetItem);
            if (element == null)
            {
                return;
            }

            var chapterTopInViewport = GetChapterTopInViewport(element);
            var adjustedOffset = Math.Max(0, _readerScrollViewer.VerticalOffset + chapterTopInViewport - ChapterTopOffset);
            _readerScrollViewer.ChangeView(null, adjustedOffset, null, true);
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => BibleChaptersListView.UpdateLayout());
            await Task.Delay(ChapterElementWaitDelayMs);
            await StabilizeReaderItemPositionAsync(scrollTargetItem);
            ApplyTopOffsetToFirstChapter();
            SuppressScrollSyncFor(TimeSpan.FromMilliseconds(NavigationScrollSyncSuppressionMs));
        }

        private async Task StabilizeReaderItemPositionAsync(BibleReaderItem targetItem)
        {
            if (targetItem == null)
            {
                return;
            }

            var stablePasses = 0;
            for (int attempt = 0; attempt < ReaderPositionStabilizationMaxAttempts; attempt++)
            {
                var element = GetChapterElement(targetItem);
                if (!IsElementForReaderItem(element, targetItem))
                {
                    // Variable-height chapter virtualization can invalidate the ListView's
                    // first estimated position and recycle the target container. Realize the
                    // intended item again before applying another exact offset correction.
                    BibleChaptersListView.ScrollIntoView(targetItem, ScrollIntoViewAlignment.Leading);
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => BibleChaptersListView.UpdateLayout());
                    await Task.Delay(LayoutSettleDelayMs);

                    element = targetItem.IsChapter
                        ? await WaitForChapterElementAsync(targetItem)
                        : await WaitForReaderItemElementAsync(targetItem);
                }

                if (element == null)
                {
                    stablePasses = 0;
                    continue;
                }

                var positionError = GetChapterTopInViewport(element) - ChapterTopOffset;
                if (Math.Abs(positionError) <= ReaderPositionTolerance)
                {
                    stablePasses++;
                    if (stablePasses >= ReaderPositionRequiredStablePasses)
                    {
                        return;
                    }
                }
                else
                {
                    stablePasses = 0;
                    var adjustedOffset = Math.Max(0, _readerScrollViewer.VerticalOffset + positionError);
                    SuppressScrollSyncFor(TimeSpan.FromMilliseconds(NavigationScrollSyncSuppressionMs));
                    _readerScrollViewer.ChangeView(null, adjustedOffset, null, true);
                }

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => BibleChaptersListView.UpdateLayout());
                await Task.Delay(LayoutSettleDelayMs);
            }
        }

        private bool ShouldPositionBookHeaderForCurrentChapter()
        {
            var currentBook = _viewModel?.CurrentBook;
            var currentChapter = _viewModel?.CurrentChapter;
            if (currentBook?.Chapters == null || currentChapter == null || currentBook.Chapters.Count == 0)
            {
                return false;
            }

            return ReferenceEquals(currentChapter, currentBook.Chapters[0]);
        }

        private async Task<FrameworkElement> WaitForReaderItemElementAsync(BibleReaderItem readerItem)
        {
            for (int attempt = 0; attempt < ChapterElementWaitMaxAttempts; attempt++)
            {
                var element = GetChapterElement(readerItem);
                if (IsElementForReaderItem(element, readerItem))
                {
                    return element;
                }

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => BibleChaptersListView.UpdateLayout());
                await Task.Delay(ChapterElementWaitDelayMs);
            }

            var finalElement = GetChapterElement(readerItem);
            return IsElementForReaderItem(finalElement, readerItem) ? finalElement : null;
        }

        private async Task<FrameworkElement> WaitForChapterElementAsync(BibleReaderItem readerItem)
        {
            for (int attempt = 0; attempt < ChapterElementWaitMaxAttempts; attempt++)
            {
                var element = GetChapterElement(readerItem);
                if (IsElementForReaderItem(element, readerItem)
                    && element.DataContext is BibleReaderItem item
                    && item.IsChapter
                    && item.ChapterIndex > 0
                    && item.DisplayLines != null
                    && item.DisplayLines.Count > 0)
                {
                    return element;
                }

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => BibleChaptersListView.UpdateLayout());
                await Task.Delay(ChapterElementWaitDelayMs);
            }

            var finalElement = GetChapterElement(readerItem);
            return IsElementForReaderItem(finalElement, readerItem) ? finalElement : null;
        }

        private static bool IsElementForReaderItem(FrameworkElement element, BibleReaderItem readerItem)
        {
            return element?.DataContext is BibleReaderItem item && ReferenceEquals(item, readerItem);
        }

        private async Task HighlightVerseAsync(int chapterIndex, int verseNumber)
        {
            if (verseNumber <= 0)
            {
                return;
            }

            var chapterItem = _viewModel.GetReaderItem(_viewModel.CurrentBook, _viewModel.CurrentBook?.Chapters?.FirstOrDefault(chapter => chapter.Index == chapterIndex));
            if (chapterItem == null)
            {
                return;
            }

            await EnsureReaderScrollViewerAsync();
            if (_readerScrollViewer == null)
            {
                return;
            }

            BibleChaptersListView.ScrollIntoView(chapterItem, ScrollIntoViewAlignment.Leading);
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => BibleChaptersListView.UpdateLayout());
            await Task.Delay(LayoutSettleDelayMs);

            var verseElement = await WaitForVerseElementAsync(chapterItem, verseNumber);
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

        private async Task<Panel> WaitForVerseElementAsync(BibleReaderItem chapterItem, int verseNumber)
        {
            for (int attempt = 0; attempt < ChapterElementWaitMaxAttempts; attempt++)
            {
                var chapterElement = await WaitForChapterElementAsync(chapterItem);
                var verseElement = FindVerseElement(chapterElement, verseNumber);
                if (verseElement != null)
                {
                    return verseElement;
                }

                BibleChaptersListView.ScrollIntoView(chapterItem, ScrollIntoViewAlignment.Leading);
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => BibleChaptersListView.UpdateLayout());
                await Task.Delay(ChapterElementWaitDelayMs);
            }

            return null;
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

        private TaskCompletionSource<bool> EnsurePendingSearchNavigationCompletionSource()
        {
            if (_pendingSearchNavigationCompletionSource == null || _pendingSearchNavigationCompletionSource.Task.IsCompleted)
            {
                _pendingSearchNavigationCompletionSource = new TaskCompletionSource<bool>();
            }

            return _pendingSearchNavigationCompletionSource;
        }

        private void CompletePendingSearchNavigation()
        {
            _pendingSearchNavigationCompletionSource?.TrySetResult(true);
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

        private IEnumerable<(BibleReaderItem ReaderItem, FrameworkElement Element, double Top)> GetRealizedChaptersOrderedByTop()
        {
            if (_viewModel?.ReaderItems == null)
            {
                yield break;
            }

            for (int i = 0; i < _viewModel.ReaderItems.Count; i++)
            {
                var readerItem = _viewModel.ReaderItems[i];
                if (readerItem?.IsChapter != true)
                {
                    continue;
                }

                var element = GetChapterElement(readerItem);
                if (element == null || element.ActualHeight <= 0)
                {
                    continue;
                }

                yield return (readerItem, element, GetChapterTopInViewport(element));
            }
        }

        private void ReaderItemGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement element)
                || !(element.DataContext is BibleReaderItem readerItem))
            {
                return;
            }

            // The chapter heading already supplies its own top margin. Avoid stacking the
            // shared item gap after a book header, which made the title-to-chapter spacing
            // noticeably larger than the spacing elsewhere in the reader.
            element.Margin = readerItem.IsBookHeader
                ? new Thickness(0)
                : new Thickness(0, 0, 0, 20);

            if (readerItem.IsChapter)
            {
                _chapterElements[readerItem] = element;
            }
        }

        private void ReaderItemGrid_Unloaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement element)
                || !(element.DataContext is BibleReaderItem readerItem)
                || !readerItem.IsChapter)
            {
                return;
            }

            if (_chapterElements.TryGetValue(readerItem, out var loadedElement) && ReferenceEquals(loadedElement, element))
            {
                _chapterElements.Remove(readerItem);
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

        private static NavigationHistoryItem CreateNavigationHistoryItem(Book book, Chapter chapter)
        {
            if (book == null || chapter == null)
            {
                return null;
            }

            var bookTitle = book.Title ?? string.Empty;
            var bookKey = Core.Dictionaries.EBookToLocation.EBookTitleToEBook.TryGetValue(bookTitle, out var bookEnum)
                ? bookEnum.ToString()
                : null;

            return new NavigationHistoryItem
            {
                BookTitle = bookTitle,
                BookKey = bookKey,
                Chapter = chapter.Index
            };
        }

        private void UpdateNavigationHistory(NavigationHistoryItem departedLocation)
        {
            try
            {
                var settingsService = App.Services.GetRequiredService<Core.Interfaces.ISettingsService>();
                var history = settingsService.GetNavigationHistory() ?? new List<NavigationHistoryItem>();
                var currentLocation = CreateNavigationHistoryItem(_viewModel.CurrentBook, _viewModel.CurrentChapter);
                var updatedHistory = NavigationHistoryManager.RecordDeparture(history, departedLocation, currentLocation);
                settingsService.SaveNavigationHistory(updatedHistory);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving navigation history: {ex.Message}");
            }
        }

        private void Window_VisibilityChanged(object sender, VisibilityChangedEventArgs args)
        {
            _isWindowVisible = args.Visible;
            UpdateReadingSessionState();
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            _isWindowActive = args.WindowActivationState != CoreWindowActivationState.Deactivated;
            UpdateReadingSessionState();
        }

        private void Application_Suspending(object sender, SuspendingEventArgs args)
        {
            _isApplicationSuspended = true;
            UpdateReadingSessionState();
        }

        private void Application_Resuming(object sender, object args)
        {
            _isApplicationSuspended = false;
            UpdateReadingSessionState();
        }

        private void AttachReadingLifecycleHandlers()
        {
            if (_areReadingLifecycleHandlersAttached)
            {
                return;
            }

            Window.Current.VisibilityChanged += Window_VisibilityChanged;
            Window.Current.Activated += Window_Activated;
            Application.Current.Suspending += Application_Suspending;
            Application.Current.Resuming += Application_Resuming;
            _areReadingLifecycleHandlersAttached = true;
        }

        private void DetachReadingLifecycleHandlers()
        {
            if (!_areReadingLifecycleHandlersAttached)
            {
                return;
            }

            Window.Current.VisibilityChanged -= Window_VisibilityChanged;
            Window.Current.Activated -= Window_Activated;
            Application.Current.Suspending -= Application_Suspending;
            Application.Current.Resuming -= Application_Resuming;
            _areReadingLifecycleHandlersAttached = false;
        }

        private void ReadingSessionTimer_Tick(object sender, object e)
        {
            FlushReadingSessionTime();
        }

        private bool IsReadingSessionEligible => ReadingSessionEligibility.ShouldCount(
            isReaderLoaded: _isLoaded,
            isReaderPageActive: _isReaderPageActive,
            isWindowVisible: _isWindowVisible,
            isWindowActive: _isWindowActive,
            isReaderObscured: _isReaderObscured,
            isApplicationSuspended: _isApplicationSuspended);

        private void UpdateReadingSessionState()
        {
            if (IsReadingSessionEligible)
            {
                StartReadingSession();
            }
            else
            {
                StopReadingSession();
            }
        }

        private void StartReadingSession()
        {
            if (!IsReadingSessionEligible)
            {
                return;
            }

            if (!_readingSessionStartedAt.HasValue)
            {
                _readingSessionStartedAt = DateTimeOffset.Now;
                _readingSessionTimer.Start();
            }

            UpdateDisplayRequest();
        }

        private void StopReadingSession()
        {
            _readingSessionTimer.Stop();
            FlushReadingSessionTime();
            _readingSessionStartedAt = null;
            ReleaseDisplayRequest();
        }

        private void UpdateDisplayRequest()
        {
            var shouldKeepScreenAwake = _isLoaded
                                        && IsReadingSessionEligible
                                        && (_viewModel.AppSettings?.KeepScreenAwake ?? false);

            if (shouldKeepScreenAwake && !_isDisplayRequestActive)
            {
                try
                {
                    _displayRequest.RequestActive();
                    _isDisplayRequestActive = true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Unable to keep the screen awake: {ex.Message}");
                }
            }
            else if (!shouldKeepScreenAwake)
            {
                ReleaseDisplayRequest();
            }
        }

        private void ReleaseDisplayRequest()
        {
            if (!_isDisplayRequestActive)
            {
                return;
            }

            try
            {
                _displayRequest.RequestRelease();
                _isDisplayRequestActive = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to release the screen-awake request: {ex.Message}");
            }
        }

        private void FlushReadingSessionTime()
        {
            if (!_readingSessionStartedAt.HasValue)
            {
                return;
            }

            var elapsed = DateTimeOffset.Now - _readingSessionStartedAt.Value;
            if (elapsed <= TimeSpan.Zero)
            {
                return;
            }

            _readingSessionStartedAt = null;

            try
            {
                var readingStreakService = App.Services.GetRequiredService<IReadingStreakService>();
                readingStreakService.AddReadingTime(elapsed);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error tracking reading time: {ex.Message}");
            }

            if (IsReadingSessionEligible)
            {
                _readingSessionStartedAt = DateTimeOffset.Now;
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
