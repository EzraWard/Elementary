using Elementary.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Elementary
{
    public sealed partial class BiblePage : Page
    {
        public BiblePageViewModel _viewModel;
        public bool _isLoaded;

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

        private void WebViewGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var gridWidth = ((Grid) sender).ActualWidth;

            if (gridWidth > 750)
            {
                ChapterView.Width = 700;
                return;
            }
            if (gridWidth < 350)
            {
                ChapterView.Width = 300;
                return;
            }

            ChapterView.Width = gridWidth - 50;
        }

        private void BibleBookChapterComboBoxes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //When changed, make sure to scroll to top
            BibleScrollViewer.ChangeView(0, 0, 1);
        }
    }
}