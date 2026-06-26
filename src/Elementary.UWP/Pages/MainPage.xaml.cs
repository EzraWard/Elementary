using CommunityToolkit.WinUI.Helpers;
using Elementary.Helpers;
using Elementary.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Elementary.Core.Enums;
using Elementary.Core.Interfaces;
using Elementary.Core.Models;
using System.Collections.Generic;
using System.Linq;
using MUXC = Microsoft.UI.Xaml.Controls;

namespace Elementary
{
    public sealed partial class MainPage : Page
    {
        private Microsoft.UI.Xaml.Controls.NavigationViewItem _lastItem;
        private IVerseOfTheDayDialogService _verseOfTheDayDialogService;
        private readonly IReadingStreakService _readingStreakService;
        private bool _isSearchPanelOpen;
        private bool _isSearchNavigationInProgress;
        private int _streakNotificationVersion;

        public MainPage()
        {
            _verseOfTheDayDialogService = App.Services.GetRequiredService<IVerseOfTheDayDialogService>();
            _readingStreakService = App.Services.GetRequiredService<IReadingStreakService>();

            this.InitializeComponent();
            _readingStreakService.ReadingActivityLogged += ReadingStreakService_ReadingActivityLogged;
            UpdateStreakNavigationIcon();

            Loaded += RedirectInitialFocus;

            Window.Current.SizeChanged += WindowSizeChanged;
            WindowSizeChanged(this, null);

            var listener = new ThemeListener();
            listener.ThemeChanged += OnThemeChanged;

            //By default, navigate to the Bible Page
            MainNavigationView.SelectedItem = BiblePageNavigationViewItem;

            Loaded += async (s, e) =>
            {
                // Wait for a short time to allow MainPage to render
                //If we don't wait, then the titlebar icon doesn't render till
                //BiblePage is navigated to and finishes intializing
                await Task.Delay(100); // Adjust delay as needed (100ms is usually enough)
                NavigateToView("BiblePage");
            };
        }

        private void RedirectInitialFocus(object sender, RoutedEventArgs e)
        {
            InitialFocusStealer.Focus(FocusState.Programmatic);
        }

        private async void ReadingStreakService_ReadingActivityLogged(object sender, ReadingStreakLoggedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                UpdateStreakNavigationIcon();
                var thresholdText = GetStreakThresholdDisplayText();
                var streakMessage = e.CurrentStreak == 1
                    ? $"You read for {thresholdText} today. You're on a 1 day streak."
                    : $"You read for {thresholdText} today. You're on a {e.CurrentStreak} day streak.";
                StreakTeachingTip.Subtitle = streakMessage;
                StreakTeachingTip.Target = StreakNavigationViewItem;
                StreakTeachingTip.IsOpen = true;

                var notificationVersion = ++_streakNotificationVersion;
                await Task.Delay(4000);
                if (notificationVersion == _streakNotificationVersion)
                {
                    StreakTeachingTip.IsOpen = false;
                }
            });
        }

        private async void NavigationView_ItemInvoked(MUXC.NavigationView sender, MUXC.NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                if (NavigateToView("SettingsPage"))
                {
                    _lastItem = null;
                }

                return;
            }

            var item = args.InvokedItemContainer as Microsoft.UI.Xaml.Controls.NavigationViewItem;
            if (item == null) return;

            var clickedView = item.Tag?.ToString();
            if (string.IsNullOrEmpty(clickedView)) return;

            if (clickedView == "VerseOfTheDay")
            {
                await _verseOfTheDayDialogService.ShowAsync();
                return;
            }

            if (clickedView == "History")
            {
                var settingsService = App.Services.GetRequiredService<ISettingsService>();
                var history = settingsService.GetNavigationHistory() ?? new List<NavigationHistoryItem>();
                var displayHistory = history.AsEnumerable().Reverse().ToList();
                HistoryListView.ItemsSource = displayHistory;

                if (displayHistory.Count == 0)
                {
                    HistoryEmptyText.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    HistoryListView.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                }
                else
                {
                    HistoryEmptyText.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    HistoryListView.Visibility = Windows.UI.Xaml.Visibility.Visible;
                }

                HistoryFlyout.ShowAt(item);
                return;
            }

            if (clickedView == "Search")
            {
                if (!SearchNavigationViewItem.IsEnabled) return;

                await SetSearchPanelOpenAsync(!_isSearchPanelOpen);
                return;
            }

            if (_lastItem != null && item == _lastItem) return;

            if (clickedView == "Settings") clickedView = "SettingsPage";
            if (!NavigateToView(clickedView)) return;
            _lastItem = item;
        }

        private bool NavigateToView(string clickedView, object parameter = null)
        {
            var view = Assembly.GetExecutingAssembly().GetType($"Elementary.{clickedView}");

            if (string.IsNullOrWhiteSpace(clickedView) || view == null) return false;

            if (parameter == null && clickedView == "BiblePage" && ContentFrame.CanGoBack)
            {
                var previousEntry = ContentFrame.BackStack.LastOrDefault();
                if (previousEntry?.SourcePageType == view)
                {
                    ContentFrame.GoBack(new EntranceNavigationTransitionInfo());
                    return true;
                }
            }

            ContentFrame.Navigate(view, parameter, new EntranceNavigationTransitionInfo());
            return true;
        }

        private void ContentFrame_Navigated(object sender, Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            UpdateCurrentPageNavigationLayout();
            UpdateSearchNavigationAvailability();
        }

        private void ContentFrame_NavigationFailed(object sender, Windows.UI.Xaml.Navigation.NavigationFailedEventArgs e)
        {

        }

        private void MainNavigationView_DisplayModeChanged(MUXC.NavigationView sender, MUXC.NavigationViewDisplayModeChangedEventArgs args)
        {
            UpdateCurrentPageNavigationLayout();
        }

        private void UpdateStreakNavigationIcon()
        {
            var hasActiveStreak = _readingStreakService.GetCurrentStreak() > 0;
            StreakNavigationIcon.FontFamily = hasActiveStreak
                ? new Windows.UI.Xaml.Media.FontFamily("Segoe UI Emoji")
                : new Windows.UI.Xaml.Media.FontFamily("Segoe UI Symbol");
            StreakNavigationIcon.Glyph = "🔥";
        }

        private string GetStreakThresholdDisplayText()
        {
            var threshold = _readingStreakService.GetDailyThreshold();
            return threshold.TotalMinutes >= 1 && Math.Abs(threshold.TotalSeconds % 60) < double.Epsilon
                ? $"{(int)threshold.TotalMinutes} minute{(threshold.TotalMinutes == 1 ? string.Empty : "s")}"
                : $"{(int)threshold.TotalSeconds} second{(threshold.TotalSeconds == 1 ? string.Empty : "s")}";
        }

        private void UpdateCurrentPageNavigationLayout()
        {
            if (ContentFrame.Content is StreakPage streakPage)
            {
                streakPage.SetHeaderInsetForMinimalNavigation(
                    MainNavigationView.DisplayMode == MUXC.NavigationViewDisplayMode.Minimal);
            }
        }

        private void UpdateSearchNavigationAvailability()
        {
            var isSearchAvailable = !(ContentFrame.Content is StreakPage) && !(ContentFrame.Content is SettingsPage);
            SearchNavigationViewItem.IsEnabled = isSearchAvailable;

            if (!isSearchAvailable && _isSearchPanelOpen)
            {
                _ = SetSearchPanelOpenAsync(false);
            }
        }

        private void WindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;

            switch (UIViewSettings.GetForCurrentView().UserInteractionMode)
            {
                case UserInteractionMode.Mouse:
                    //VisualStateManager.GoToState(this, "MouseLayout", true);
                    TitlebarGrid.Visibility = Visibility.Visible;
                    TitleBarRow.Height = new GridLength(32);

                    break;

                case UserInteractionMode.Touch:
                    //VisualStateManager.GoToState(this, "TouchLayout", true);

                    TitlebarGrid.Visibility = Visibility.Collapsed;
                    TitleBarRow.Height = new GridLength(0);

                    //set caption button colors
                    //titleBar.BackgroundColor = Color.FromArgb(255, 39, 39, 39);

                    //For some reason, UWP seems to ignore this when the app's primary mode is tablet.
                    //So, I just turned it off, and will allow the default color to stay.
                    //titleBar.ButtonBackgroundColor = Color.FromArgb(255, 39, 39, 39); 
                    break;

                default:
                    break;

            }
        }

        private void OnThemeChanged(ThemeListener sender)
        {
            UpdateStreakNavigationIcon();
            if (SystemInformationHelper.Instance.OperatingSystemVersion.Build <= 20348)
            {
                WindowHelpers.SetCaptionButtonColors(sender.CurrentTheme);
            }
        }

        private async void HistoryListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e?.ClickedItem is NavigationHistoryItem item)
            {
                if (ContentFrame.Content is BiblePage biblePage)
                {
                    await biblePage.NavigateToFromHistoryAsync(item.BookTitle, item.Chapter, item.BookKey);
                }
                else if (!NavigateToView("BiblePage", item))
                {
                    return;
                }

                HistoryFlyout.Hide();
                // Update last item to Bible Page navigation item
                _lastItem = BiblePageNavigationViewItem;
                MainNavigationView.SelectedItem = _lastItem;
            }
        }

        private Task SetSearchPanelOpenAsync(bool isOpen)
        {
            _isSearchPanelOpen = isOpen;
            SearchPanel.Visibility = _isSearchPanelOpen ? Visibility.Visible : Visibility.Collapsed;
            if (_isSearchPanelOpen)
            {
                SearchBox.Focus(FocusState.Programmatic);
                return Task.CompletedTask;
            }

            ClearSearchResultSelection();
            ClearSearchEmptyState();
            ClearActiveSearchHighlight();
            return Task.CompletedTask;
        }

        private async void CloseSearchPanelButton_Click(object sender, RoutedEventArgs e)
        {
            await SetSearchPanelOpenAsync(false);
        }

        private async void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (_isSearchNavigationInProgress) return;

            var query = args.QueryText?.Trim();
            if (string.IsNullOrWhiteSpace(query)) return;

            await ExecuteSearchAsync(query);
        }

        private async Task ExecuteSearchAsync(string query)
        {
            SearchResultsListView.Visibility = Visibility.Collapsed;
            SearchEmptyText.Text = "No results";
            SearchEmptyText.Visibility = Visibility.Collapsed;
            SearchProgressRing.IsActive = true;

            try
            {
                var scopeItem = SearchScopeComboBox.SelectedItem as ComboBoxItem;
                var scopeTag = scopeItem?.Tag?.ToString() ?? "EntireBible";
                var scope = ESearchScope.EntireBible;
                switch (scopeTag)
                {
                    case "OldTestament":
                        scope = ESearchScope.OldTestament;
                        break;
                    case "NewTestament":
                        scope = ESearchScope.NewTestament;
                        break;
                }

                var searchService = App.Services.GetRequiredService<ISearchService>();
                var bibleService = App.Services.GetRequiredService<IBibleService>();
                var settingsService = App.Services.GetRequiredService<ISettingsService>();
                var settings = settingsService.GetSettings();
                var bible = await bibleService.GetBible(settings.Translation);
                var results = await searchService.SearchAsync(bible, settings.Translation, query, scope);

                if (results.Count == 0)
                {
                    SearchEmptyText.Visibility = Visibility.Visible;
                    SearchResultsListView.Visibility = Visibility.Collapsed;
                    SearchResultsListView.ItemsSource = null;
                }
                else
                {
                    SearchResultsListView.ItemsSource = results;
                    SearchResultsListView.Visibility = Visibility.Visible;
                    SearchEmptyText.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Search failed: {ex.Message}");
                SearchResultsListView.ItemsSource = null;
                SearchResultsListView.Visibility = Visibility.Collapsed;
                SearchEmptyText.Text = "Search failed";
                SearchEmptyText.Visibility = Visibility.Visible;
            }
            finally
            {
                SearchProgressRing.IsActive = false;
            }
        }

        private async void SearchResultsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (_isSearchNavigationInProgress || !(e?.ClickedItem is SearchResult result)) return;

            var searchParam = new SearchNavigationParameter
            {
                BookTitle = result.BookTitle,
                BookKey = result.BookKey,
                ChapterIndex = result.ChapterIndex,
                VerseNumber = result.VerseNumber,
                SearchQuery = SearchBox.Text?.Trim()
            };

            SetSearchNavigationState(isBusy: true);
            try
            {
                var biblePage = await GetOrNavigateToBiblePageAsync();
                if (biblePage == null)
                {
                    return;
                }

                await biblePage.NavigateToFromSearchAsync(searchParam);

                if (!_isSearchPanelOpen)
                {
                    biblePage.ClearSearchHighlight();
                }

                _lastItem = BiblePageNavigationViewItem;
                MainNavigationView.SelectedItem = _lastItem;
            }
            finally
            {
                SetSearchNavigationState(isBusy: false);
            }
        }

        private void SetSearchNavigationState(bool isBusy)
        {
            _isSearchNavigationInProgress = isBusy;
            SearchResultsListView.IsEnabled = !isBusy;
            SearchBox.IsEnabled = !isBusy;
            SearchScopeComboBox.IsEnabled = !isBusy;
            SearchProgressRing.IsActive = isBusy;
        }

        private async Task<BiblePage> GetOrNavigateToBiblePageAsync()
        {
            if (ContentFrame.Content is BiblePage existingBiblePage)
            {
                return existingBiblePage;
            }

            if (!NavigateToView("BiblePage"))
            {
                return null;
            }

            for (int attempt = 0; attempt < 20; attempt++)
            {
                await Task.Delay(25);
                if (ContentFrame.Content is BiblePage biblePage)
                {
                    return biblePage;
                }
            }

            return ContentFrame.Content as BiblePage;
        }

        private void ClearActiveSearchHighlight()
        {
            if (ContentFrame.Content is BiblePage biblePage)
            {
                biblePage.ClearSearchHighlight();
            }
        }

        private void ClearSearchResultSelection()
        {
            SearchResultsListView.SelectedItem = null;
        }

        private void ClearSearchEmptyState()
        {
            SearchProgressRing.IsActive = false;
            SearchEmptyText.Text = "No results";
            SearchEmptyText.Visibility = Visibility.Collapsed;
        }
    }
}
