using Elementary.VerseOfTheDay.Interfaces;
using Elementary.VerseOfTheDay.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Elementary.Services
{
    public class VerseOfTheDayDialogService : IVerseOfTheDayDialogService
    {
        private readonly IVerseOfTheDayService _verseOfTheDayService;

        public VerseOfTheDayDialogService(IVerseOfTheDayService verseOfTheDayService)
        {
            _verseOfTheDayService = verseOfTheDayService ?? throw new ArgumentNullException(nameof(verseOfTheDayService));
        }

        public async Task ShowAsync()
        {
            var dialog = new ContentDialog
            {
                Title = $"Verse of the Day for {DateTime.Now.ToShortDateString()}",
                CloseButtonText = "Close",
                DefaultButton = ContentDialogButton.Close,
                RequestedTheme = ((FrameworkElement)Window.Current.Content).RequestedTheme
            };

            var imageBrush = new ImageBrush
            {
                Stretch = Stretch.Uniform
            };

            var image = new Border
            {
                Width = 490,
                Height = 490,
                CornerRadius = new CornerRadius(10),
                Background = imageBrush,
                Visibility = Visibility.Collapsed
            };

            var loadingRing = new ProgressRing
            {
                IsActive = true,
                Width = 48,
                Height = 48,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var failureText = new TextBlock
            {
                Text = "Unable to load verse image.",
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed
            };

            var container = new Grid
            {
                MinWidth = 500,
                MinHeight = 500
            };
            container.Children.Add(image);
            container.Children.Add(loadingRing);
            container.Children.Add(failureText);

            dialog.Content = container;

            // Fetch and display composited image without blocking the dialog opening
            _ = LoadImageAsync(image, imageBrush, loadingRing, failureText);

            await dialog.ShowAsync();
        }

        private async Task LoadImageAsync(
            Border image,
            ImageBrush imageBrush,
            ProgressRing loadingRing,
            TextBlock failureText)
        {
            try
            {
                var result = await _verseOfTheDayService.GetAsync(VotdImageSize.InApp);

                if (result.ImageBytes != null && result.ImageBytes.Length > 0)
                {
                    var bitmap = new BitmapImage();
                    using var ms = new MemoryStream(result.ImageBytes);
                    using var ras = ms.AsRandomAccessStream();
                    await bitmap.SetSourceAsync(ras);

                    imageBrush.ImageSource = bitmap;
                    loadingRing.IsActive = false;
                    loadingRing.Visibility = Visibility.Collapsed;
                    image.Visibility = Visibility.Visible;
                }
                else
                {
                    ShowFailure(loadingRing, failureText);
                }
            }
            catch (Exception)
            {
                ShowFailure(loadingRing, failureText);
            }
        }

        private static void ShowFailure(ProgressRing loadingRing, TextBlock failureText)
        {
            loadingRing.IsActive = false;
            loadingRing.Visibility = Visibility.Collapsed;
            failureText.Visibility = Visibility.Visible;
        }
    }
}
