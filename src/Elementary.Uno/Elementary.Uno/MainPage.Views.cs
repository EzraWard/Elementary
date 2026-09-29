using Elementary.Core.Enums;
using Elementary.Core.Models;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Elementary.Uno;

public sealed partial class MainPage
{
    private ScrollViewer CreateSettingsView()
    {
        _initializingSettings = true;
        var panel = new StackPanel { Spacing = 4, MaxWidth = 1000, Margin = new Thickness(16) };
        panel.Children.Add(SectionHeading("Reading"));
        panel.Children.Add(SettingsCard("Translation", "The translation of the Bible you would like to use",
            new TextBlock { Text = "KJV", VerticalAlignment = VerticalAlignment.Center }, "\uE736"));
        var numbers = new ToggleSwitch { IsOn = _settings.ShowVerseNumbers ?? true };
        numbers.Toggled += (_, _) => { if (_initializingSettings) return; _settings.ShowVerseNumbers = numbers.IsOn; ApplyReaderSettings(); };
        panel.Children.Add(SettingsCard("Show verse numbers", "Choose whether to show verse numbers inline", numbers, "\uE8A4"));
        var font = new ComboBox { ItemsSource = new[] { "Segoe UI", "Georgia" }, SelectedIndex = _settings.Font == EFont.Georgia ? 1 : 0 };
        font.SelectionChanged += (_, _) => { if (_initializingSettings || font.SelectedIndex < 0) return; _settings.Font = font.SelectedIndex == 1 ? EFont.Georgia : EFont.SegoeUIVariable; ApplyReaderSettings(); };
        panel.Children.Add(SettingsCard("Font", "Select the font for text", font, "\uE8D2"));
        var size = new ComboBox { ItemsSource = new[] { "Small", "Medium", "Large" }, SelectedIndex = _settings.FontSize == EFontSize.Small ? 0 : _settings.FontSize == EFontSize.Large ? 2 : 1 };
        size.SelectionChanged += (_, _) => { if (_initializingSettings || size.SelectedIndex < 0) return; _settings.FontSize = size.SelectedIndex == 0 ? EFontSize.Small : size.SelectedIndex == 2 ? EFontSize.Large : EFontSize.Medium; ApplyReaderSettings(); };
        panel.Children.Add(SettingsCard("Font size", "Select the font size for text", size, "\uE8E9"));
        panel.Children.Add(SectionHeading("General"));
        var theme = new ComboBox { ItemsSource = new[] { "System", "Light", "Dark" }, SelectedIndex = _settings.Theme == ETheme.Light ? 1 : _settings.Theme == ETheme.Dark ? 2 : 0 };
        theme.SelectionChanged += (_, _) =>
        {
            if (_initializingSettings || theme.SelectedIndex < 0) return;
            _settings.Theme = theme.SelectedIndex == 1 ? ETheme.Light : theme.SelectedIndex == 2 ? ETheme.Dark : ETheme.System;
            ApplyReaderSettings();
        };
        panel.Children.Add(SettingsCard("Theme", "Set the color scheme you prefer", theme, "\uE790"));
        panel.Children.Add(SectionHeading("About"));
        var about = new StackPanel { Spacing = 8 };
        about.Children.Add(new Image { Source = new BitmapImage(new Uri("ms-appx:///Assets/Elementary.png")), Width = 48, Height = 48, HorizontalAlignment = HorizontalAlignment.Left });
        about.Children.Add(new TextBlock { Text = "Elementary", FontSize = 20, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        about.Children.Add(new TextBlock { Text = "© 2026. All rights reserved.", Opacity = 0.75 });
        about.Children.Add(new TextBlock { Text = "Version 1.0.0 • Web", Opacity = 0.75 });
        panel.Children.Add(Card(about));
        panel.Children.Add(new HyperlinkButton { Content = "Send feedback", NavigateUri = new Uri("mailto:ezra.ward@outlook.com") });
        _initializingSettings = false;
        return new ScrollViewer { Content = panel, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
    }

    private ScrollViewer CreateStreakView()
    {
        var current = _streakService.GetCurrentStreak();
        var best = _streakService.GetLongestStreak();
        var badges = StreakBadgeCatalog.BuildProgress(best);
        var panel = new StackPanel { MaxWidth = 1000, Margin = new Thickness(16), Spacing = 16 };
        panel.Children.Add(new TextBlock { Text = "Reading Streak", FontSize = 32, FontWeight = Microsoft.UI.Text.FontWeights.Bold });
        panel.Children.Add(new TextBlock
        {
            Text = best == 0 ? "Actively read the Bible for 10 minutes today to start your streak."
                : "Your streak grows when you actively read the Bible for at least 10 minutes on consecutive calendar days.",
            TextWrapping = TextWrapping.Wrap, Opacity = 0.8
        });
        var cards = new Grid { ColumnSpacing = 12, RowSpacing = 12 };
        var wide = ActualWidth >= 720;
        cards.ColumnDefinitions.Add(new ColumnDefinition { Width = wide ? new GridLength(320) : new GridLength(1, GridUnitType.Star) });
        if (wide) cards.ColumnDefinitions.Add(new ColumnDefinition());
        cards.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        cards.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var hero = new StackPanel { Spacing = 10 };
        hero.Children.Add(new TextBlock { Text = current.ToString(), FontSize = 72, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, TextAlignment = TextAlignment.Center, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 214, 118, 6)) });
        hero.Children.Add(new TextBlock { Text = "day streak", TextAlignment = TextAlignment.Center, Opacity = 0.85 });
        var days = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Spacing = 8 };
        var activity = _streakService.GetRecentActivity(7);
        for (var i = 0; i < 7; i++)
        {
            var day = new StackPanel { Spacing = 6 };
            day.Children.Add(new TextBlock { Text = DateTime.Today.AddDays(i - 6).ToString("ddd"), FontSize = 12, TextAlignment = TextAlignment.Center });
            day.Children.Add(new TextBlock { Text = activity[i] ? "●" : "○", FontSize = 20, TextAlignment = TextAlignment.Center, Opacity = activity[i] ? 1 : 0.4 });
            days.Children.Add(day);
        }
        hero.Children.Add(days);
        var progress = _streakService.GetProgress();
        progress.DailyReadingSeconds.TryGetValue(DateTime.Today, out var seconds);
        hero.Children.Add(new TextBlock { Text = $"Today: {seconds / 60} of 10 minutes", TextAlignment = TextAlignment.Center, Opacity = 0.75 });
        cards.Children.Add(Card(hero));
        var details = new StackPanel { Spacing = 12 };
        details.Children.Add(new TextBlock { Text = "Your progress", FontSize = 20, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        details.Children.Add(new TextBlock { Text = $"Current streak     {current} days\nLongest streak     {best} days", FontSize = 18 });
        details.Children.Add(new TextBlock { Text = "A day counts after 10 minutes with the Bible open and Elementary active.", TextWrapping = TextWrapping.Wrap, Opacity = 0.8 });
        details.Children.Add(new TextBlock { Text = "Open the Bible, settle in, and read for 10 minutes each day. Come back tomorrow to keep the flame glowing! If you miss a day, your current streak starts fresh, but your best streak stays safe.", TextWrapping = TextWrapping.Wrap, Opacity = 0.8 });
        var detailCard = Card(details);
        Grid.SetColumn(detailCard, wide ? 1 : 0);
        Grid.SetRow(detailCard, wide ? 0 : 1);
        cards.Children.Add(detailCard);
        panel.Children.Add(cards);
        panel.Children.Add(new TextBlock { Text = "Streak badges", FontSize = 20, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        var next = badges.FirstOrDefault(badge => badge.IsNextToEarn);
        panel.Children.Add(new TextBlock { Text = next is null ? "You've unlocked every current streak badge." : $"Next badge: {next.Title} at {next.ThresholdDays} day{(next.ThresholdDays == 1 ? "" : "s")}.", Opacity = 0.8 });
        var badgeGrid = new Grid { ColumnSpacing = 12, RowSpacing = 12 };
        var columns = wide ? 2 : 1;
        for (var col = 0; col < columns; col++) badgeGrid.ColumnDefinitions.Add(new ColumnDefinition());
        for (var row = 0; row < (badges.Count + columns - 1) / columns; row++) badgeGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        for (var i = 0; i < badges.Count; i++)
        {
            var badge = badges[i];
            var content = new StackPanel { Spacing = 4 };
            content.Children.Add(new TextBlock { Text = $"{badge.PlaceholderText}   {badge.Title}", FontSize = 16, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            content.Children.Add(new TextBlock { Text = badge.Description, TextWrapping = TextWrapping.Wrap, Opacity = 0.78 });
            content.Children.Add(new TextBlock { Text = badge.StatusText, FontSize = 12, Opacity = 0.65 });
            var card = Card(content);
            Grid.SetColumn(card, i % columns);
            Grid.SetRow(card, i / columns);
            badgeGrid.Children.Add(card);
        }
        panel.Children.Add(badgeGrid);
        return new ScrollViewer { Content = panel, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
    }

    private static TextBlock SectionHeading(string text) => new()
    {
        Text = text, Margin = new Thickness(1, 30, 0, 6), FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
    };

    private Border Card(UIElement content)
    {
        var card = new Border { Padding = new Thickness(16), CornerRadius = new CornerRadius(8), Child = content };
        void UpdateBackground() => card.Background = new SolidColorBrush(card.ActualTheme == ElementTheme.Light
            ? Windows.UI.Color.FromArgb(255, 255, 255, 255) : Windows.UI.Color.FromArgb(255, 43, 43, 43));
        card.ActualThemeChanged += (_, _) => UpdateBackground();
        card.Loaded += (_, _) => UpdateBackground();
        UpdateBackground();
        return card;
    }

    private Border SettingsCard(string title, string description, FrameworkElement control, string glyph)
    {
        var grid = new Grid { ColumnSpacing = 16, MinHeight = 48 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var icon = new FontIcon { FontFamily = new FontFamily("ms-appx:///Assets/Fonts/Segoe Fluent Icons.ttf#Segoe Fluent Icons"), Glyph = glyph, FontSize = 20 };
        grid.Children.Add(icon);
        var label = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Spacing = 2 };
        label.Children.Add(new TextBlock { Text = title, TextWrapping = TextWrapping.Wrap });
        label.Children.Add(new TextBlock { Text = description, TextWrapping = TextWrapping.Wrap, FontSize = 12, Opacity = 0.75 });
        Grid.SetColumn(label, 1);
        grid.Children.Add(label);
        control.VerticalAlignment = VerticalAlignment.Center;
        control.MinWidth = 100;
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(control, title);
        Grid.SetColumn(control, 2);
        grid.Children.Add(control);
        return Card(grid);
    }
}
