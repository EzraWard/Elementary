using Elementary.Core.Enums;
using Elementary.Core.Interfaces;
using Elementary.Core.Models;
using Elementary.Core.Services;
using Elementary.Uno.Services;
using Elementary.VerseOfTheDay.Models;
using Elementary.VerseOfTheDay.Services;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Diagnostics;

namespace Elementary.Uno;

public sealed partial class MainPage
{
    private readonly SettingsService _settingsService = new(new LocalSettingsProvider());
    private Elementary.Core.Interfaces.ISettings _settings = null!;
    private ReadingStreakService _streakService = null!;
    private NavigationHistoryItem? _currentLocation;
    private readonly DispatcherTimer _readingTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly Stopwatch _readingClock = new();
    private bool _windowActive = true;
    private bool _historyOpen;
    private bool _dialogOpen;
    private string _currentPage = "Bible";
    private bool _initializingSettings;

    private void InitializeShell()
    {
        _settings = _settingsService.GetSettings();
        _settings.Translation = ETranslation.KJV;
        _streakService = new ReadingStreakService(_settingsService);
        MainNavigationView.SelectedItem = BibleNavigationItem;
        ApplyReaderSettings();
        _readingTimer.Tick += ReadingTimer_Tick;
        Loaded += (_, _) => { _readingClock.Restart(); _readingTimer.Start(); };
        Unloaded += (_, _) => { _readingTimer.Stop(); _readingClock.Stop(); };
        if (App.MainWindow is { } window)
        {
            window.Activated += (_, e) =>
            {
                _windowActive = e.WindowActivationState != Windows.UI.Core.CoreWindowActivationState.Deactivated;
                _readingClock.Restart();
            };
        }
        SizeChanged += (_, _) =>
        {
            MainNavigationView.PaneDisplayMode = ActualWidth < 640
                ? NavigationViewPaneDisplayMode.LeftMinimal : NavigationViewPaneDisplayMode.LeftCompact;
            SearchPanel.Width = Math.Min(320, Math.Max(200, ActualWidth - 90));
        };
    }

    private async void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked)
        {
            ShowPage("Settings");
            return;
        }
        var tag = (args.InvokedItemContainer as NavigationViewItem)?.Tag as string;
        switch (tag)
        {
            case "Bible": ShowPage("Bible"); break;
            case "Search": ToggleSearch(); break;
            case "History":
                var history = _settingsService.GetNavigationHistory().AsEnumerable().Reverse().ToList();
                HistoryList.ItemsSource = history;
                HistoryEmptyText.Visibility = history.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                HistoryFlyout.ShowAt(HistoryNavigationItem);
                break;
            case "Streak": ShowPage("Streak"); break;
            case "VerseOfTheDay": await ShowVerseOfTheDayAsync(); break;
        }
    }

    private void ShowPage(string page)
    {
        _readingClock.Restart();
        _currentPage = page;
        var reading = page == "Bible";
        ReaderLayout.Visibility = reading ? Visibility.Visible : Visibility.Collapsed;
        SecondaryContent.Visibility = reading ? Visibility.Collapsed : Visibility.Visible;
        SearchNavigationItem.IsEnabled = reading;
        HistoryNavigationItem.IsEnabled = reading;
        if (!reading)
        {
            SearchPanel.Visibility = Visibility.Collapsed;
            HistoryFlyout.Hide();
        }
        if (page == "Streak") SecondaryContent.Content = CreateStreakView();
        if (page == "Settings") SecondaryContent.Content = CreateSettingsView();
        if (reading) MainNavigationView.SelectedItem = BibleNavigationItem;
        if (MainNavigationView.DisplayMode == NavigationViewDisplayMode.Minimal)
            MainNavigationView.IsPaneOpen = false;
    }

    private void RecordLocation(int chapter)
    {
        var location = new NavigationHistoryItem { BookTitle = "Genesis", BookKey = "GEN", Chapter = chapter };
        _settingsService.SaveNavigationHistory(NavigationHistoryManager.RecordDeparture(
            _settingsService.GetNavigationHistory(), _currentLocation, location));
        _currentLocation = location;
        _settings.Chapter = chapter;
        _settingsService.SaveSettings(_settings);
    }

    private void HistoryList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not NavigationHistoryItem item) return;
        ShowPage("Bible");
        BookChapterComboBox.SelectedItem = item.Chapter;
        HistoryFlyout.Hide();
    }

    private void HistoryFlyout_Opened(object sender, object e) => _historyOpen = true;
    private void HistoryFlyout_Closed(object sender, object e) => _historyOpen = false;

    private void ScrollToVerse(int verse)
    {
        // Resolve the rendered row after the chapter's ItemsControl has laid out.
        DispatcherQueue.TryEnqueue(() =>
        {
            VerseList.UpdateLayout();
            var line = _readerLines.FirstOrDefault(row => row.Number == verse.ToString());
            if (line is null) return;
            var index = _readerLines.IndexOf(line);
            if (VerseList.ContainerFromIndex(index) is FrameworkElement container)
                container.StartBringIntoView(new BringIntoViewOptions { VerticalAlignmentRatio = 0.25 });
        });
    }

    private void ReadingTimer_Tick(object? sender, object e)
    {
        var elapsed = _readingClock.Elapsed;
        _readingClock.Restart();
        // A suspended or throttled browser must not accrue time for an inactive reader.
        if (_currentPage != "Bible" || !_windowActive || !BrowserActivity.HasFocus() || _historyOpen || _dialogOpen
            || SearchPanel.Visibility == Visibility.Visible || _genesis is null
            || elapsed.TotalSeconds > 2.5) return;
        _streakService.AddReadingTime(TimeSpan.FromSeconds(1));
    }

    private async Task ShowVerseOfTheDayAsync()
    {
        if (_dialogOpen) return;
        _dialogOpen = true;
        var image = new Image { Width = 490, Height = 490, Stretch = Stretch.Uniform, Visibility = Visibility.Collapsed };
        var loading = new ProgressRing { IsActive = true, Width = 40, Height = 40 };
        var failure = new TextBlock { Text = "Unable to load verse image.", TextWrapping = TextWrapping.Wrap, Visibility = Visibility.Collapsed };
        var content = new Grid { MinHeight = 260, MaxWidth = 490 };
        content.Children.Add(image);
        content.Children.Add(loading);
        content.Children.Add(failure);
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot, RequestedTheme = RequestedTheme,
            Title = $"Verse of the Day for {DateTime.Now.ToShortDateString()}",
            CloseButtonText = "Close", Content = content
        };
        try
        {
            var show = dialog.ShowAsync();
            try
            {
                var day = DateTime.UtcNow.ToString("yyyy-MM-dd");
                var cache = new LocalSettingsProvider();
                BibleVerseData? verse = null;
                var json = cache.GetSetting("votd-verse");
                if (cache.GetSetting("votd-date") == day && !string.IsNullOrEmpty(json))
                    verse = System.Text.Json.JsonSerializer.Deserialize(json, VerseJsonContext.Default.BibleVerseData);
                if (verse is null)
                {
                    using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(8) };
                    verse = await new VerseFetchService(client).FetchAsync();
                    cache.SaveSetting("votd-date", day);
                    cache.SaveSetting("votd-verse", System.Text.Json.JsonSerializer.Serialize(verse, VerseJsonContext.Default.BibleVerseData));
                }
                var bytes = new VotdImageCompositor().Compose(verse, VotdImageSize.InApp, day);
                var bitmap = new BitmapImage();
                using var stream = new MemoryStream(bytes);
                await bitmap.SetSourceAsync(stream.AsRandomAccessStream());
                image.Source = bitmap;
                image.Visibility = Visibility.Visible;
            }
            catch (Exception)
            {
                failure.Visibility = Visibility.Visible;
            }
            finally { loading.IsActive = false; loading.Visibility = Visibility.Collapsed; }
            await show;
        }
        finally { _dialogOpen = false; _readingClock.Restart(); }
    }

    private void ApplyReaderSettings()
    {
        RequestedTheme = _settings.Theme switch
        {
            ETheme.Light => ElementTheme.Light,
            ETheme.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
        _settingsService.SaveSettings(_settings);
        if (_genesis is null || BookChapterComboBox.SelectedItem is not int chapterNumber) return;
        var chapter = _genesis.Chapters.FirstOrDefault(chapter => chapter.Index == chapterNumber);
        if (chapter is null) return;
        _readerLines.Clear();
        foreach (var line in chapter.ToDisplayLines()) _readerLines.Add(new ReaderLine(line, _settings));
        var size = _settings.FontSize == EFontSize.Small ? 14 : _settings.FontSize == EFontSize.Large ? 22 : 18;
        BookHeading.FontSize = size * 1.9;
        ChapterHeading.FontSize = size * 1.55;
        BookHeading.FontFamily = ChapterHeading.FontFamily = new FontFamily(_settings.Font == EFont.Georgia ? "ms-appx:///Assets/Fonts/GeorgiaBold.ttf#Georgia" : "ms-appx:///Assets/Fonts/SegoeUIBold.ttf#Segoe UI");
    }
}
