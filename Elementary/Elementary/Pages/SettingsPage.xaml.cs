using Windows.ApplicationModel;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

namespace Elementary
{
    public sealed partial class SettingsPage : Page
    {
        bool IsLoaded = false;

        public SettingsPage()
        {
            InitializeComponent();

            VersionTextBlock.Text = GetApplicationVersion();

            Loaded += SettingsPage_Loaded;
        }

        private void SettingsPage_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            IsLoaded = true;
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
    }
}