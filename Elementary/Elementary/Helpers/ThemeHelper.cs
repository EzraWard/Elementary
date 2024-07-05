namespace Elementary.Helpers
{
    public static class ThemeHelpers
    {
        public static string GetCurrentApplicationTheme()
        {
            var DefaultTheme = new Windows.UI.ViewManagement.UISettings();
            var uiTheme = DefaultTheme.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background).ToString();
            if (uiTheme == "#FF000000")
            {
                return "Dark";
            }
            else if (uiTheme == "#FFFFFFFF")
            {
                return "Light";
            }

            return "Unknown";
        }
    }
}