using Elementary.Core.Models;
using Elementary.ViewModels;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Elementary
{
    public sealed partial class BiblePage : Page
    {
        private BiblePageViewModel _viewModel;
        private bool _isLoaded;
        private double _previousVerticalOffset = 0;
        private bool _isUpdatingFromScroll = false;
        private bool _isAwaitingChapterSelection = false;
        private bool _suppressComboHandling = false;
        private bool _ignoreNextChapterSelectionChange = false;
        private readonly TranslateTransform _chooserTranslate = new TranslateTransform();
        private Book _committedBook;
        private int _committedChapterIndex = 1;

        public BiblePage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Required;
            Loaded += BiblePage_Loaded;
            ChooserBorder.RenderTransform = _chooserTranslate;

            _viewModel = new BiblePageViewModel();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (!_isLoaded || _viewModel == null) return;

            // Re-read settings so font/size changes from Settings page apply immediately
            _viewModel.RefreshSettings();
            ReapplyTypographyToAllElements();
        }

        private void ReapplyTypographyToAllElements()
        {
            if (_viewModel == null) return;

            try
            {
                for (int i = 0; i < _viewModel.Chapters.Count; i++)
                {
                    var element = ChaptersRepeater.TryGetElement(i);
                    if (element is FrameworkElement fe)
                    {
                        ApplyReadingTypography(fe);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reapplying typography: {ex.Message}");
            }
        }

        private async void BiblePage_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = _viewModel;
            await _viewModel.Initialize();
            UpdateCommittedSelection();

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
                firstElement.Margin = new Thickness(0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying top offset: {ex.Message}");
            }
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
            ApplyReadingTypographyRecursive(root, fontFamily, fontSize);
        }

        private void ApplyReadingTypographyRecursive(DependencyObject node, FontFamily fontFamily, double fontSize)
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
                ApplyReadingTypographyRecursive(VisualTreeHelper.GetChild(node, i), fontFamily, fontSize);
            }
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
                    if (!_isAwaitingChapterSelection)
                    {
                        UpdateCommittedSelection();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating chapter from scroll: {ex.Message}");
            }
        }

        private void BibleBookComboBox_DropDownOpened(object sender, object e)
        {
            if (!_isLoaded || _isAwaitingChapterSelection) return;
            UpdateCommittedSelection();
        }

        private void BibleScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (!_isLoaded) return;

            var scrollViewer = (ScrollViewer)sender;
            var verticalOffset = scrollViewer.VerticalOffset;

            var delta = verticalOffset - _previousVerticalOffset;
            UpdateChooserPosition(delta, verticalOffset);

            // Update the current chapter based on scroll position
            if (!e.IsIntermediate)
            {
                UpdateCurrentChapterFromScroll();
            }

            _previousVerticalOffset = verticalOffset;
        }

        private void UpdateChooserPosition(double scrollDelta, double verticalOffset)
        {
            if (ChooserBorder == null) return;

            var hiddenOffset = ChooserBorder.ActualHeight + ChooserBorder.Margin.Top + 2;
            if (hiddenOffset <= 0) return;

            if (verticalOffset <= 0)
            {
                _chooserTranslate.Y = 0;
                return;
            }

            if (scrollDelta <= 0)
            {
                _chooserTranslate.Y = 0;
                return;
            }

            var next = _chooserTranslate.Y - scrollDelta;
            if (next > 0) next = 0;
            if (next < -hiddenOffset) next = -hiddenOffset;
            _chooserTranslate.Y = next;
        }

        private async void BibleBookChapterComboBoxes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded || _isUpdatingFromScroll || _suppressComboHandling) return;

            if (sender == BibleBookComboBox)
            {
                _isAwaitingChapterSelection = true;
                _ignoreNextChapterSelectionChange = true;
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    BookChapterComboBox.IsDropDownOpen = true;
                });
                return;
            }

            if (sender == BookChapterComboBox && _isAwaitingChapterSelection)
            {
                if (!BookChapterComboBox.IsDropDownOpen)
                {
                    // Ignore programmatic chapter changes caused by selecting a new book.
                    return;
                }

                if (_ignoreNextChapterSelectionChange)
                {
                    _ignoreNextChapterSelectionChange = false;
                    return;
                }

                _isAwaitingChapterSelection = false;
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
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving navigation history: {ex.Message}");
            }

            // Reload chapters from the selected position
            await _viewModel.LoadInitialChaptersAsync();
             
            // Scroll to the current chapter
            ScrollToCurrentChapter();
            UpdateCommittedSelection();
        }

        private async void BookChapterComboBox_DropDownClosed(object sender, object e)
        {
            if (!_isLoaded || !_isAwaitingChapterSelection) return;

            _isAwaitingChapterSelection = false;
            _ignoreNextChapterSelectionChange = false;
            _suppressComboHandling = true;
            try
            {
                _viewModel.CurrentBook = _committedBook;
                _viewModel.SelectedChapterIndex = _committedChapterIndex;
                await _viewModel.LoadInitialChaptersAsync();
                ScrollToCurrentChapter();
                UpdateCommittedSelection();
            }
            finally
            {
                _suppressComboHandling = false;
            }
        }

        public async void NavigateToFromHistory(string bookTitle, int chapter)
        {
            // If page isn't fully initialized yet, defer until Loaded finishes
            if (!_isLoaded)
            {
                Loaded += async (s, e) =>
                {
                    await _viewModel.UpdateNavigationSettingsAsync(bookTitle, chapter);
                    await _viewModel.LoadInitialChaptersAsync();
                    ScrollToCurrentChapter();
                };
                return;
            }

            await _viewModel.UpdateNavigationSettingsAsync(bookTitle, chapter);
            await _viewModel.LoadInitialChaptersAsync();
            ScrollToCurrentChapter();
            UpdateCommittedSelection();
        }

        private void UpdateCommittedSelection()
        {
            _committedBook = _viewModel.CurrentBook;
            _committedChapterIndex = _viewModel.SelectedChapterIndex;
        }

    }
}
