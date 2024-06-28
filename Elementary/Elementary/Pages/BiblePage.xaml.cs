using Elementary.ViewModels;
using Elementary.Objects;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Elementary
{
    public sealed partial class BiblePage : Page
    {
        public BiblePageViewModel _viewModel;
        public bool _isLoaded = false;

        public BiblePage()
        {
            _viewModel = new BiblePageViewModel();
            DataContext = _viewModel;

            InitializeComponent();

            //VM intialization
            _viewModel.Initialize();

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
            _viewModel.Book = book;
            _viewModel.Chapter = book.Chapters.FirstOrDefault();

            BookChapterComboBox.ItemsSource = _viewModel.Book.Chapters;
            BookChapterComboBox.SelectedItem = _viewModel.Chapter;
        }

        private void BookChapterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            var comboBox = sender as ComboBox;
            var selectedItem = comboBox.SelectedItem as Chapter;
            if (selectedItem is null) return;

            _viewModel.SetCurrentChapterContent(selectedItem.ReadingOrderIndex);
        }
    }
}