using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using MUXC = Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls;
using System.Reflection;
using Windows.UI.Xaml.Media.Animation;

namespace Elementary
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Microsoft.UI.Xaml.Controls.NavigationViewItem _lastItem;

        public MainPage()
        {
            this.InitializeComponent();

            NavigateToView("BiblePage");
        }

        private void NavigationView_ItemInvoked(MUXC.NavigationView sender, MUXC.NavigationViewItemInvokedEventArgs args)
        {
            var item = args.InvokedItemContainer as Microsoft.UI.Xaml.Controls.NavigationViewItem;
            if (item == null || item == _lastItem) return;

            var clickedView = item.Tag.ToString();
            if (clickedView == null || clickedView == "Settings") clickedView = "SettingsPage";

            if(!NavigateToView(clickedView)) return;
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
    }
}
