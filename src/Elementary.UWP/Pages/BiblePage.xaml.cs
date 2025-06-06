using Elementary.ViewModels;
using Elementary.Core.Models;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Storage;

namespace Elementary
{
    public sealed partial class BiblePage : Page
    {
        public BiblePageViewModel _viewModel;
        public bool _isLoaded = false;

        public BiblePage()
        {
            _viewModel = new BiblePageViewModel();

            InitializeComponent();

            //VM intialization
            _viewModel.Initialize();

            DataContext = _viewModel;

            Loaded += BiblePage_Loaded;
        }

        private void BiblePage_Loaded(object sender, RoutedEventArgs e)
        {
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

        private void BibleBookComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            var comboBox = sender as ComboBox;
            var book = (Book) comboBox.SelectedItem;
            _viewModel.CurrentBook = book;
            _viewModel.CurrentChapter = book.Chapters.FirstOrDefault();

            //scroll to top
            BibleScrollViewer.ChangeView(0, 0, 1);
        }

        private void BookChapterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            var comboBox = sender as ComboBox;
            var selectedItem = comboBox.SelectedItem;

            //scroll to top
            BibleScrollViewer.ChangeView(0, 0, 1);
        }
    }
}