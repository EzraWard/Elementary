using CommunityToolkit.WinUI.Helpers;
using Elementary.Helpers;
using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using MUXC = Microsoft.UI.Xaml.Controls;

namespace Elementary
{
    public sealed partial class MainPage : Page
    {
        private Microsoft.UI.Xaml.Controls.NavigationViewItem _lastItem;

        public MainPage()
        {
            this.InitializeComponent();

            Window.Current.SizeChanged += WindowSizeChanged;
            WindowSizeChanged(this, null);

            var listener = new ThemeListener();
            listener.ThemeChanged += OnThemeChanged;

            //By default, navigate to the Bible Page
            MainNavigationView.SelectedItem = BiblePageNavigationViewItem;

            Loaded += async (s, e) =>
            {
                // Wait for a short time to allow MainPage to render
                //If we don't wait, then the titlebar icon doesn't render till
                //BiblePage is navigated to and finishes intializing
                await Task.Delay(100); // Adjust delay as needed (100ms is usually enough)
                NavigateToView("BiblePage");
            };
        }

        private void NavigationView_ItemInvoked(MUXC.NavigationView sender, MUXC.NavigationViewItemInvokedEventArgs args)
        {
            var item = args.InvokedItemContainer as Microsoft.UI.Xaml.Controls.NavigationViewItem;
            if (item == null || item == _lastItem) return;

            var clickedView = item.Tag.ToString();
            if (clickedView == null || clickedView == "Settings") clickedView = "SettingsPage";

            if (clickedView == "VerseOfTheDay")
            {
                ContentDialog dialog = new ContentDialog();
                dialog.Title = "Verse of the Day for " + DateTime.Now.ToShortDateString();
                dialog.CloseButtonText = "Close";
                dialog.DefaultButton = ContentDialogButton.Primary;

                var image = new Image
                {
                    Height = 500,
                    Width = 500,
                };

                var todayMonth = DateTime.Now.ToString("MM");
                var todayDay = DateTime.Now.ToString("dd");

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.UriSource = new Uri(@"https://votd.olivetree.com/" + todayMonth + "_" + todayDay + "_NKJV.jpg");
                image.Source = bitmapImage;

                dialog.Content = image;

                var result = dialog.ShowAsync();
            }

            if (!NavigateToView(clickedView)) return;
            _lastItem = item;
        }

        private bool NavigateToView(string clickedView)
        {
            var view = Assembly.GetExecutingAssembly().GetType($"Elementary.{clickedView}");

            if (string.IsNullOrWhiteSpace(clickedView) || view == null) return false;

            ContentFrame.Navigate(view, null, new EntranceNavigationTransitionInfo());
            return true;
        }

        private void ContentFrame_NavigationFailed(object sender, Windows.UI.Xaml.Navigation.NavigationFailedEventArgs e)
        {

        }

        private void WindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;

            switch (UIViewSettings.GetForCurrentView().UserInteractionMode)
            {
                case UserInteractionMode.Mouse:
                    //VisualStateManager.GoToState(this, "MouseLayout", true);
                    TitlebarGrid.Visibility = Visibility.Visible;
                    TitleBarRow.Height = new GridLength(32);

                    break;

                case UserInteractionMode.Touch:
                    //VisualStateManager.GoToState(this, "TouchLayout", true);

                    TitlebarGrid.Visibility = Visibility.Collapsed;
                    TitleBarRow.Height = new GridLength(0);

                    //set caption button colors
                    //titleBar.BackgroundColor = Color.FromArgb(255, 39, 39, 39);

                    //For some reason, UWP seems to ignore this when the app's primary mode is tablet.
                    //So, I just turned it off, and will allow the default color to stay.
                    //titleBar.ButtonBackgroundColor = Color.FromArgb(255, 39, 39, 39); 
                    break;

                default:
                    break;

            }
        }

        private void OnThemeChanged(ThemeListener sender)
        {
            if (SystemInformationHelper.Instance.OperatingSystemVersion.Build <= 20348)
            {
                WindowHelpers.SetCaptionButtonColors(sender.CurrentTheme);
            }
        }
    }
}
