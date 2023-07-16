using Elementary.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
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

        public BiblePage()
        {
            _viewModel = new BiblePageViewModel();
            DataContext = _viewModel;

            InitializeComponent();

            //VM intialization
            _viewModel.Initialize();

            ChapterView.NavigateToString(_viewModel.CurrentChapterContent);
            //ChapterView.Navigate(new Uri("https://www.windowscentral.com"));
            //ChapterView.NavigationCompleted += ConfigureWebview();
            //ConfigureWebview();

            BibleBookComboBox.SelectedIndex = 0;
            BookChapterComboBox.SelectedIndex = 0;

            ChapterView.NavigationCompleted += ConfigureWebview;
        }

        private async void ConfigureWebview(WebView sender, WebViewNavigationCompletedEventArgs e)
        {
            await ChapterView.InvokeScriptAsync("eval", new string[] { "document.body.style.font=\"18px Segoe UI, sans-serif\"" });
            await ChapterView.InvokeScriptAsync("eval", new string[] { "document.body.style.color=\"#FFFFFF\"" });
            //await ChapterView.InvokeScriptAsync("eval", new string[] { "document.body.style.msOverflowStyle='scrollbar'" });

            //Determine height of current content, and set the webview height to it.
            var contentHeight = await ChapterView.InvokeScriptAsync("eval", new string[] { "document.body.scrollHeight.toString()" });

            if (int.TryParse(contentHeight, out int height))
            {
                // Update the WebView's height to match the HTML content
                ChapterView.Height = height;
            }


        }

        private static string[] SetBodyOverFlowHiddenString = new string[] { "document.body.style.overflow = \"hidden\";" };
        private static string[] SetFontSizeString = new string[] { "document.getElementsByTagName(\"p\")[0].style.fontSize=\"" + 30 + "\";" };
        private static string[] test = new string[] { @"function setScrollbar()
                                                        {
                                                            //document.body.style.overflow = 'hidden';  
                                                            document.body.style.msOverflowStyle='scrollbar';   
                                                        }" };
    }
}

