using Elementary.Services;
using Elementary.VerseOfTheDay.Interfaces;
using Elementary.VerseOfTheDay.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace Elementary
{
    public sealed partial class VerseOfTheDayPage : Page
    {
        private readonly IVerseOfTheDayService _votdService;
        private readonly ITileUpdateService _tileService;

        public VerseOfTheDayPage()
        {
            _votdService = App.Services.GetRequiredService<IVerseOfTheDayService>();
            _tileService = App.Services.GetRequiredService<ITileUpdateService>();
            this.InitializeComponent();
#if DEBUG
            RefreshButton.Visibility = Visibility.Visible;
#endif
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await LoadVerseAsync();
        }

        private async Task LoadVerseAsync()
        {
            SetLoadingState();
            try
            {
                Debug.WriteLine("[VerseOfTheDayPage] Loading Verse of the Day.");
                var result = await _votdService.GetAsync(VotdImageSize.InApp);
                await ShowResultAsync(result);

                // Update live tiles whenever the VOTD page is shown
                await _tileService.UpdateAsync(result);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VerseOfTheDayPage] Load failed: {ex}");
                SetErrorState();
            }
        }

        private async Task ShowResultAsync(VerseOfTheDayResult result)
        {
            LoadingRing.IsActive = false;
            LoadingRing.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Collapsed;

            if (result.ImageBytes != null && result.ImageBytes.Length > 0)
            {
                var bitmap = new BitmapImage();
                using var ms = new MemoryStream(result.ImageBytes);
                using var ras = ms.AsRandomAccessStream();
                await bitmap.SetSourceAsync(ras);
                VerseImage.Source = bitmap;
                VerseImage.Visibility = Visibility.Visible;
            }

            Debug.WriteLine($"[VerseOfTheDayPage] Showing generated VOTD image. Bytes={result.ImageBytes?.Length ?? 0}.");
        }

        private void SetLoadingState()
        {
            LoadingRing.IsActive = true;
            LoadingRing.Visibility = Visibility.Visible;
            ErrorPanel.Visibility = Visibility.Collapsed;
            VerseImage.Visibility = Visibility.Collapsed;
        }

        private void SetErrorState()
        {
            LoadingRing.IsActive = false;
            LoadingRing.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Visible;
            VerseImage.Visibility = Visibility.Collapsed;
        }

#if DEBUG
        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[VerseOfTheDayPage] Debug refresh requested. Clearing today's VOTD cache.");
            _votdService.InvalidateToday();
            await LoadVerseAsync();
        }
#else
        private void RefreshButton_Click(object sender, RoutedEventArgs e) { }
#endif
    }
}
