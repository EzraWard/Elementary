using Elementary.Core.Models;
using Elementary.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace Elementary
{
    public sealed partial class ReadingPlansPage : Page
    {
        public ReadingPlansPageViewModel ViewModel { get; }

        public ReadingPlansPage()
        {
            InitializeComponent();
            ViewModel = new ReadingPlansPageViewModel();
            DataContext = ViewModel;
            Loaded += ReadingPlansPage_Loaded;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel.Refresh();
            SyncSelectedPlan();
        }

        private void ReadingPlansPage_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.Initialize();
            SyncSelectedPlan();
        }

        private void PlansListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PlansListView.SelectedItem is ReadingPlan plan)
            {
                ViewModel.SelectedPlan = plan;
            }
        }

        private void StartOrRestartPlanButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartOrRestartSelectedPlan();
            SyncSelectedPlan();
        }

        private void MarkCurrentReadingCompleteButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CompleteCurrentDay();
        }

        private void VisibleDayPassagesListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (!(e.ClickedItem is ReadingPlanPassage passage))
            {
                return;
            }

            var navigationItem = new NavigationHistoryItem
            {
                BookTitle = passage.BookTitle,
                BookKey = passage.BookKey,
                Chapter = passage.Chapter
            };

            if ((Window.Current.Content as Frame)?.Content is MainPage mainPage)
            {
                mainPage.NavigateToBiblePage(navigationItem);
                return;
            }

            Frame.Navigate(typeof(BiblePage), navigationItem, new EntranceNavigationTransitionInfo());
        }

        private void SyncSelectedPlan()
        {
            if (ViewModel.SelectedPlan != null && !ReferenceEquals(PlansListView.SelectedItem, ViewModel.SelectedPlan))
            {
                PlansListView.SelectedItem = ViewModel.SelectedPlan;
            }
        }
    }
}
