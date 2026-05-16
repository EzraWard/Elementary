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
        private const double ChapterTopOffset = 96d;
        private const int LayoutSettleDelayMs = 100;
        private const int NavigationSpinnerDelayMs = 120;
        private const int NavigationScrollSyncSuppressionMs = 500;
        private const int IntermediateScrollSyncThrottleMs = 75;
        private const int ComboResetMaxAttempts = 5;
        private const int ComboResetRetryDelayMs = 20;

        private readonly BiblePageViewModel _viewModel;
        private bool _isLoaded;
        private bool _isInitializing;
        private bool _isUpdatingFromScroll;
        private bool _isAwaitingChapterSelection;
        private bool _isAdjustingChapterWindow;
        private bool _suppressComboHandling;
        private DateTimeOffset _ignoreScrollSyncUntil = DateTimeOffset.MinValue;
        private readonly TranslateTransform _chooserTranslate = new TranslateTransform();
        private readonly Dictionary<Chapter, FrameworkElement> _chapterElements = new Dictionary<Chapter, FrameworkElement>();
        private string _pendingHistoryBook;
        private int _pendingHistoryChapter;
        private string _pendingHistoryBookKey;
        private int _navigationVisualStateVersion;
        private DateTimeOffset _lastIntermediateScrollSyncAt = DateTimeOffset.MinValue;

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
                SetPickerInteractionEnabled(false);
                SetReaderVisualState(showContent: false, showSpinner: true);

                await _viewModel.Initialize();
                ResetChapterElementTracking();
                _chooserTranslate.Y = 0;
                SetupTopFadeGradient();

                if (_pendingHistoryBook != null)
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
                var element = GetChapterElement(currentChapterIndex);
                if (element != null)
                {
                    var targetOffset = GetChapterOffsetInContent(element);
                    targetOffset = Math.Max(0, targetOffset - ChapterTopOffset);
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
            if (!_isLoaded || _isAwaitingChapterSelection || _viewModel.Chapters.Count == 0 || _isAdjustingChapterWindow) return;

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
                        await EnsureCurrentChapterWindowAsync(preserveViewport: true);
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
                var element = GetChapterElement(i);
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

        private async void BibleScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (!_isLoaded) return;

            _chooserTranslate.Y = 0;

            if (DateTimeOffset.UtcNow < _ignoreScrollSyncUntil)
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

            await UpdateCurrentChapterFromScrollAsync();
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
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => BibleScrollViewer.UpdateLayout());
                await Task.Delay(LayoutSettleDelayMs);
            }

            ScrollToCurrentChapter();
        }

        private FrameworkElement GetChapterElement(int chapterIndex)
        {
            var chapter = _viewModel?.Chapters?.ElementAtOrDefault(chapterIndex);
            if (chapter == null)
            {
                return null;
            }

            return _chapterElements.TryGetValue(chapter, out var element) ? element : null;
        }

        private async Task EnsureCurrentChapterWindowAsync(bool preserveViewport)
        {
            if (_viewModel.CurrentChapter == null)
            {
                return;
            }

            _isAdjustingChapterWindow = true;
            try
            {
                var currentChapter = _viewModel.CurrentChapter;
                var currentChapterIndex = _viewModel.Chapters.IndexOf(currentChapter);
                var currentChapterElement = currentChapterIndex >= 0 ? GetChapterElement(currentChapterIndex) : null;
                var currentChapterTopBefore = currentChapterElement != null
                    ? GetChapterTopInViewport(currentChapterElement)
                    : (double?)null;
                var currentOffsetBefore = BibleScrollViewer.VerticalOffset;

                var chapterWindowChanged = await _viewModel.EnsureCurrentChapterWindowAsync();
                if (!chapterWindowChanged)
                {
                    return;
                }

                await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () => BibleScrollViewer.UpdateLayout());

                if (!preserveViewport)
                {
                    return;
                }

                var refreshedChapterIndex = _viewModel.Chapters.IndexOf(currentChapter);
                var refreshedChapterElement = refreshedChapterIndex >= 0 ? GetChapterElement(refreshedChapterIndex) : null;
                if (currentChapterTopBefore.HasValue && refreshedChapterElement != null)
                {
                    var currentChapterTopAfter = GetChapterTopInViewport(refreshedChapterElement);
                    var adjustedOffset = Math.Max(0, currentOffsetBefore + (currentChapterTopAfter - currentChapterTopBefore.Value));
                    SuppressScrollSyncFor(TimeSpan.FromMilliseconds(250));
                    BibleScrollViewer.ChangeView(null, adjustedOffset, null, true);
                }
            }
            finally
            {
                _isAdjustingChapterWindow = false;
            }
        }

        private double GetChapterOffsetInContent(FrameworkElement chapterElement)
        {
            if (chapterElement == null)
            {
                return 0d;
            }

            var transform = chapterElement.TransformToVisual(ChaptersContainer);
            var chapterPosition = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
            return chapterPosition.Y;
        }

        private double GetChapterTopInViewport(FrameworkElement chapterElement)
        {
            if (chapterElement == null)
            {
                return 0d;
            }

            var transform = chapterElement.TransformToVisual(BibleScrollViewer);
            var chapterPosition = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
            return chapterPosition.Y;
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
            SetPickerInteractionEnabled(false);
            SetReaderVisualState(showContent: false, showSpinner: false);
            _ = ShowNavigationSpinnerIfStillPendingAsync(navigationVersion);

            try
            {
                await navigationAction();
            }
            finally
            {
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
            BibleScrollViewer.Opacity = showContent ? 1 : 0;
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
