using Elementary.Core.Enums;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace Elementary.Helpers
{
    public static class ThemeHelpers
    {
        public static ApplicationTheme GetCurrentApplicationTheme()
        {
            var uiSettings = new UISettings();
            var background = uiSettings.GetColorValue(UIColorType.Background);
            var perceivedBrightness = ((background.R * 299) + (background.G * 587) + (background.B * 114)) / 1000;

            return perceivedBrightness < 128
                ? ApplicationTheme.Dark
                : ApplicationTheme.Light;
        }

        public static ApplicationTheme ResolveApplicationTheme(ETheme theme)
        {
            switch (theme)
            {
                case ETheme.Dark:
                    return ApplicationTheme.Dark;
                case ETheme.Light:
                    return ApplicationTheme.Light;
                case ETheme.System:
                case ETheme.NotSet:
                default:
                    return GetCurrentApplicationTheme();
            }
        }

        public static ApplicationTheme ApplyTheme(ETheme theme)
        {
            var applicationTheme = ResolveApplicationTheme(theme);
            ApplyTheme(applicationTheme);
            return applicationTheme;
        }

        public static void ApplyTheme(ApplicationTheme applicationTheme)
        {
            if (Window.Current?.Content is FrameworkElement rootElement)
            {
                // Application.RequestedTheme cannot change after startup, so System must be
                // resolved and applied explicitly to the root element at runtime.
                rootElement.RequestedTheme = applicationTheme == ApplicationTheme.Dark
                    ? ElementTheme.Dark
                    : ElementTheme.Light;
            }

            WindowHelpers.SetCaptionButtonColors(applicationTheme);
        }
    }
}
