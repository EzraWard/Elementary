using Elementary.ViewModels;
using Elementary.Core.Models;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Storage;
using Windows.UI.Xaml.Controls.Primitives;

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
            _viewModel.Book = book;
            _viewModel.Chapter = book.Chapters.FirstOrDefault();

            var Settings = ApplicationData.Current.LocalSettings;
            Settings.Values["Book"] = _viewModel.Book.Title;
            Settings.Values["Chapter"] = _viewModel.Chapter.Index;

            BookChapterComboBox.ItemsSource = _viewModel.Book.Chapters;
            BookChapterComboBox.SelectedItem = _viewModel.Chapter;

            //scroll to top
            BibleScrollViewer.ChangeView(0, 0, 1);
        }

        private void BookChapterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            var comboBox = sender as ComboBox;
            var selectedItem = comboBox.SelectedItem as Chapter;
            if (selectedItem is null) return;

            var Settings = ApplicationData.Current.LocalSettings;
            Settings.Values["Chapter"] = selectedItem.Index.ToString();

            _viewModel.SetCurrentChapterContent(selectedItem.ReadingOrderIndex);

            //scroll to top
            BibleScrollViewer.ChangeView(0, 0, 1);
        }
    }
}