using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace Elementary.Helpers
{
    public static class WindowHelpers
    {
        public  static void SetCaptionButtonColors(ApplicationTheme currentTheme)
        {
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            // Let the extended title bar and dialog overlay show through the caption buttons.
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            switch (currentTheme)
            {
                case ApplicationTheme.Dark:
                    titleBar.BackgroundColor = Color.FromArgb(255, 32, 32, 32);
                    break;

                case ApplicationTheme.Light:
                    titleBar.BackgroundColor = Color.FromArgb(255, 243, 243, 243);
                    break;
            }
        }
    }
}
