using Elementary.ViewModels;
using Windows.UI.Xaml.Controls;

namespace Elementary
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPageViewModel ViewModel { get; private set; }

        public SettingsPage()
        {
            InitializeComponent();
            
            ViewModel = new SettingsPageViewModel();
            
            DataContext = ViewModel;
        }
    }
}