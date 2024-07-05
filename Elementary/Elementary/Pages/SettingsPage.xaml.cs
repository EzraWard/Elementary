using System.Linq;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

namespace Elementary
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();

            VersionTextBlock.Text = GetApplicationVersion();

            var settings = ApplicationData.Current.LocalSettings;
            var font = settings.Values["Font"];
            var fontSize = settings.Values["FontSize"];
            var theme = settings.Values["Theme"];

            FontComboBox.SelectedItem = FontComboBox.Items.Where(i => i.ToString() == font.ToString()).FirstOrDefault();
            FontSizeComboBox.SelectedItem = FontSizeComboBox.Items.Where(i => i.ToString() == fontSize.ToString()).FirstOrDefault();
            ThemeComboBox.SelectedItem = ThemeComboBox.Items.Where(i => i.ToString() == theme.ToString()).FirstOrDefault();
        }

        public static string GetApplicationVersion()
        {
            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;

            return string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build);
        }

        private void TranslationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            var comboBox = (ComboBox) sender;

            var Settings = ApplicationData.Current.LocalSettings;
            Settings.Values["Translation"] = ((ComboBoxItem) comboBox.SelectedItem).Content;
            Settings.Values["Book"] = "Genesis";
            Settings.Values["Chapter"] = "1";
        }

        private void FontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            var comboBox = (ComboBox)sender;
        }

        private void FontSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            var comboBox = (ComboBox)sender;

            var fontSize = ((ComboBoxItem)comboBox.SelectedItem).Content;
            var settings = ApplicationData.Current.LocalSettings;
            settings.Values["FontSize"] = fontSize;
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            var comboBox = (ComboBox)sender;

            var theme = ((ComboBoxItem)comboBox.SelectedItem).Content;
            var settings = ApplicationData.Current.LocalSettings;
            settings.Values["Theme"] = theme;
        }
    }
}