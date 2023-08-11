using Elementary.ViewModels;
using Elementary.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elementary
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
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

            ChapterView.NavigateToString(_viewModel.CurrentChapterContent);

            ChapterView.NavigationCompleted += ConfigureWebview;

            _isLoaded = true;
        }

        private async void ConfigureWebview(WebView sender, WebViewNavigationCompletedEventArgs e)
        {
            await ChapterView.InvokeScriptAsync("eval", new string[] { "document.body.style.font=\"18px Segoe UI, sans-serif\"" });
            await ChapterView.InvokeScriptAsync("eval", new string[] { "document.body.style.color=\"#FFFFFF\"" });
            await ChapterView.InvokeScriptAsync("eval", new string[] { "document.body.style.overflow = \"hidden\"" });

            await ResizeWebViewToContent(ChapterView);
        }

        private async void WebViewGrid_SizeChanged(object sender, SizeChangedEventArgs e)
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

            await ResizeWebViewToContent(ChapterView);
        }

        private async Task ResizeWebViewToContent(WebView webView)
        {
            //Determine height of current content, and set the webview height to it.
            var contentHeight = await webView.InvokeScriptAsync("eval", new string[] { "document.body.scrollHeight.toString()" });

            if (int.TryParse(contentHeight, out int height))
            {
                // Update the WebView's height to match the HTML content
                webView.Height = height;
            }
        }

        private static string[] SetBodyOverFlowHiddenString = new string[] { "document.body.style.overflow = \"hidden\";" };
        private static string[] SetFontSizeString = new string[] { "document.getElementsByTagName(\"p\")[0].style.fontSize=\"" + 30 + "\";" };
        private static string[] DisableScroll = new string[] { @"function setScrollbar()
                                                        {
                                                            document.body.style.overflow = 'hidden';  
                                                            //document.body.style.msOverflowStyle='scrollbar';   
                                                        }" };

        private void BibleBookComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            var comboBox = sender as ComboBox;
            var book = (Book) comboBox.SelectedItem;
            _viewModel.Book = book;
            _viewModel.Chapter = book.Chapters.FirstOrDefault();

            BookChapterComboBox.ItemsSource = _viewModel.Book.Chapters;
            BookChapterComboBox.SelectedItem = _viewModel.Chapter;
            ChapterView.NavigateToString(_viewModel.CurrentBible.ReadingOrder[_viewModel.Book.ReadingOrderIndex + 1].Content);
        }

        private void BookChapterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            var comboBox = sender as ComboBox;
            var selectedItem = comboBox.SelectedItem as Chapter;
            if (selectedItem is null) return;

            _viewModel.SetCurrentChapterContent(selectedItem.ReadingOrderIndex);
            ChapterView.NavigateToString(_viewModel.CurrentChapterContent);

        }
    }
}

