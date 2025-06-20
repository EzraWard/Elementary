using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Elementary.UWP.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public bool IsInverted { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                // If IsInverted is true, flip the logic
                bool result = IsInverted ? !boolValue : boolValue;
                return result ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility visibility)
            {
                bool result = visibility == Visibility.Visible;
                return IsInverted ? !result : result;
            }

            return false;
        }
    }
}