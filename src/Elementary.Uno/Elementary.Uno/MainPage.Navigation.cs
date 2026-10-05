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
        if (_settings.Translation == ETranslation.NotSet) _settings.Translation = ETranslation.KJV;
        _catalog.SetTranslation(_settings.Translation);
        _streakService = new ReadingStreakService(_settingsService);
        Loaded += (_, _) => DispatcherQueue.TryEnqueue(() =>
        {
            MainNavigationView.UpdateLayout();
            MainNavigationView.SelectedItem = BibleNavigationItem;
            BibleNavigationItem.InvalidateMeasure();
            BibleNavigationItem.UpdateLayout();
        });
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
        if (reading && _readerNeedsPosition && _book is not null)
        {
            _readerNeedsPosition = false;
            _ = NavigateToBookAsync(_book, _settings.Chapter);
        }
        if (MainNavigationView.DisplayMode == NavigationViewDisplayMode.Minimal)
            MainNavigationView.IsPaneOpen = false;
    }

    private void RecordLocation(int chapter)
    {
        var location = new NavigationHistoryItem { BookTitle = _book!.Title, BookKey = _book.Key, Chapter = chapter };
        _settingsService.SaveNavigationHistory(NavigationHistoryManager.RecordDeparture(
            _settingsService.GetNavigationHistory(), _currentLocation, location));
        _currentLocation = location;
        _settings.Book = _book.Id;
        _settings.Chapter = chapter;
        _settingsService.SaveSettings(_settings);
    }

    private async void HistoryList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not NavigationHistoryItem item) return;
        ShowPage("Bible");
        var book = _catalog.Books.FirstOrDefault(book => book.Key == item.BookKey || book.Title == item.BookTitle);
        if (book is not null) await NavigateToBookAsync(book, item.Chapter);
        HistoryFlyout.Hide();
    }

    private void HistoryFlyout_Opened(object sender, object e) => _historyOpen = true;
    private void HistoryFlyout_Closed(object sender, object e) => _historyOpen = false;

    private async Task ShowNavigationLoadingAsync(int version)
    {
        await Task.Delay(250);
        if (version == _loadVersion && _positioningReader) NavigationLoading.Visibility = Visibility.Visible;
    }

    private ReaderLine? _highlightedLine;
    private int _highlightVersion;
    private async Task HighlightVerseAsync(ReaderLine line)
    {
        var version = ++_highlightVersion;
        _highlightedLine?.Highlight(false);
        _highlightedLine = line;
        line.Highlight(true);
        await Task.Delay(2200);
        if (version == _highlightVersion) { line.Highlight(false); _highlightedLine = null; }
    }

    private void ScrollToVerse(int verse)
    {
        // Resolve the rendered row after the chapter's ItemsControl has laid out.
        DispatcherQueue.TryEnqueue(() =>
        {
            VerseList.UpdateLayout();
            var line = _readerWindow.FirstOrDefault(chapter => chapter.Book == _book && chapter.Chapter.Index == _settings.Chapter)?.Lines.FirstOrDefault(row => row.Number == verse.ToString());
            if (line is null) return;
            _ = HighlightVerseAsync(line);
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
            || SearchPanel.Visibility == Visibility.Visible || _loadedBook is null || _positioningReader || _pendingBook is not null
            || elapsed.TotalSeconds > 2.5) return;
        _streakService.AddReadingTime(TimeSpan.FromSeconds(1));
    }

    private async Task ShowVerseOfTheDayAsync()
    {
        if (_dialogOpen) return;
        _dialogOpen = true;
        var image = new Image { MaxWidth = 490, MaxHeight = 490, Stretch = Stretch.Uniform, Visibility = Visibility.Collapsed };
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
        async Task LoadImageAsync(bool refresh)
        {
            image.Visibility = Visibility.Collapsed;
            failure.Visibility = Visibility.Collapsed;
            loading.Visibility = Visibility.Visible;
            loading.IsActive = true;
            dialog.IsPrimaryButtonEnabled = false;
            if (refresh)
            {
                var cache = new LocalSettingsProvider();
                cache.DeleteSetting("votd-date");
                cache.DeleteSetting("votd-verse");
            }
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
                Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(image, $"{verse.Reference}. {verse.VerseText}");
                image.Visibility = Visibility.Visible;
            }
            catch (Exception)
            {
                failure.Visibility = Visibility.Visible;
            }
            finally { loading.IsActive = false; loading.Visibility = Visibility.Collapsed; }
            dialog.IsPrimaryButtonEnabled = true;
        }
#if DEBUG
        dialog.PrimaryButtonText = "Refresh";
        dialog.PrimaryButtonClick += async (_, args) =>
        {
            args.Cancel = true;
            var deferral = args.GetDeferral();
            try { await LoadImageAsync(true); }
            finally { deferral.Complete(); }
        };
#endif
        try
        {
            var show = dialog.ShowAsync();
            await LoadImageAsync(false);
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
        RefreshReaderTypography();
    }
}
