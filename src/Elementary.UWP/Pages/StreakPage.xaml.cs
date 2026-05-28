using Elementary.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Elementary
{
    public sealed partial class StreakPage : Page
    {
        public StreakPageViewModel ViewModel { get; }

        public StreakPage()
        {
            InitializeComponent();
            ViewModel = new StreakPageViewModel();
            DataContext = ViewModel;
            Loaded += StreakPage_Loaded;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel.Refresh();
        }

        private void StreakPage_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.Initialize();
        }
    }
}
