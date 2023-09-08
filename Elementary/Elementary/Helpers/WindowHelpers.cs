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

            switch (currentTheme)
            {
                case ApplicationTheme.Dark:
                    titleBar.BackgroundColor = Color.FromArgb(255, 32, 32, 32);
                    titleBar.ButtonBackgroundColor = Color.FromArgb(255, 32, 32, 32);
                    titleBar.ButtonInactiveBackgroundColor = Color.FromArgb(255, 32, 32, 32);
                    break;

                case ApplicationTheme.Light:
                    titleBar.BackgroundColor = Color.FromArgb(255, 243, 243, 243);
                    titleBar.ButtonBackgroundColor = Color.FromArgb(255, 243, 243, 243);
                    titleBar.ButtonInactiveBackgroundColor = Color.FromArgb(255, 243, 243, 243);
                    break;
            }
        }
    }
}