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

            _previousVerticalOffset = verticalOffset;
        }

        private void BibleBookChapterComboBoxes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;

            // Reload chapters from the selected position
            _viewModel.LoadInitialChapters();
            
            // Scroll to top
            BibleScrollViewer.ChangeView(0, 0, 1);
        }
    }
}