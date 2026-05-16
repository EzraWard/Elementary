using Elementary.Services;
using Elementary.VerseOfTheDay.Interfaces;
using Elementary.VerseOfTheDay.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
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
                var result = await _votdService.GetAsync(VotdImageSize.InApp);
                ShowResult(result);

                // Update live tiles whenever the VOTD page is shown
                await _tileService.UpdateAsync(result);
            }
            catch (Exception)
            {
                SetErrorState();
            }
        }

        private void ShowResult(VerseOfTheDayResult result)
        {
            LoadingRing.IsActive = false;
            LoadingRing.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Collapsed;

            if (result.ImageBytes != null && result.ImageBytes.Length > 0)
            {
                var bitmap = new BitmapImage();
                using var ms = new MemoryStream(result.ImageBytes);
                using var ras = ms.AsRandomAccessStream();
                // BitmapImage.SetSourceAsync must be awaited, but page code-behind uses fire-and-forget
                _ = bitmap.SetSourceAsync(ras);
                VerseImage.Source = bitmap;
                VerseImage.Visibility = Visibility.Visible;
            }

            if (!string.IsNullOrEmpty(result.UnsplashAttribution))
            {
                AttributionText.Text = result.UnsplashAttribution;
                AttributionText.Visibility = Visibility.Visible;
            }
        }

        private void SetLoadingState()
        {
            LoadingRing.IsActive = true;
            LoadingRing.Visibility = Visibility.Visible;
            ErrorPanel.Visibility = Visibility.Collapsed;
            VerseImage.Visibility = Visibility.Collapsed;
            AttributionText.Visibility = Visibility.Collapsed;
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
            await LoadVerseAsync();
        }
#else
        private void RefreshButton_Click(object sender, RoutedEventArgs e) { }
#endif
    }
}
