using Elementary.ViewModels;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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
        }

        private void ChapterItemGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var grid = (Grid)sender;
            var richTextBlock = grid.Children[0] as RichTextBlock;
            if (richTextBlock == null) return;

            // Set font properties from ViewModel
            richTextBlock.FontFamily = new Windows.UI.Xaml.Media.FontFamily(_viewModel.Font);
            richTextBlock.FontSize = _viewModel.FontSize;

            var gridWidth = grid.ActualWidth;

            if (gridWidth > 750)
            {
                richTextBlock.Width = 700;
                return;
            }
            if (gridWidth < 350)
            {
                richTextBlock.Width = 300;
                return;
            }

            richTextBlock.Width = gridWidth - 50;
        }

        private void UpdateCurrentChapterFromScroll()
        {
            if (!_isLoaded || _viewModel.Chapters.Count == 0) return;

            // Find which chapter is currently most visible in the viewport
            try
            {
                var scrollViewer = BibleScrollViewer;
                var viewportTop = scrollViewer.VerticalOffset;
                var viewportCenter = viewportTop + (scrollViewer.ViewportHeight / 2);

                // Iterate through the visual tree to find which chapter grid is at viewport center
                double cumulativeHeight = 0;
                int visibleChapterIndex = 0;

                for (int i = 0; i < _viewModel.Chapters.Count; i++)
                {
                    var element = ChaptersRepeater.TryGetElement(i);
                    if (element is Grid grid)
                    {
                        var nextHeight = cumulativeHeight + grid.ActualHeight + 32; // 32 is the margin
                        if (viewportCenter >= cumulativeHeight && viewportCenter < nextHeight)
                        {
                            visibleChapterIndex = i;
                            break;
                        }
                        cumulativeHeight = nextHeight;
                    }
                }

                var visibleChapter = _viewModel.Chapters[visibleChapterIndex];
                
                // Update the ViewModel's current chapter if different
                if (_viewModel.CurrentChapter != visibleChapter)
                {
                    _isUpdatingFromScroll = true;
                    _viewModel.UpdateCurrentChapterFromScroll(visibleChapter);
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

        private void BibleBookChapterComboBoxes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded || _isUpdatingFromScroll) return;

            // Reload chapters from the selected position
            _viewModel.LoadInitialChapters();
            
            // Scroll to top
            BibleScrollViewer.ChangeView(0, 0, 1);
        }
    }
}