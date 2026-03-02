using System;
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
            var verse = _verseOfTheDayService.GetVerseOfTheDay();

            var dialog = new ContentDialog
            {
                Title = verse.Title,
                CloseButtonText = "Close",
                DefaultButton = ContentDialogButton.Primary
            };

            var image = new Image
            {
                Width = 500,
                Height = 500,
                Stretch = Stretch.Uniform,
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

            image.ImageOpened += (s, e) =>
            {
                loadingRing.IsActive = false;
                loadingRing.Visibility = Visibility.Collapsed;
                failureText.Visibility = Visibility.Collapsed;
                image.Visibility = Visibility.Visible;
            };

            image.ImageFailed += (s, e) =>
            {
                loadingRing.IsActive = false;
                loadingRing.Visibility = Visibility.Collapsed;
                image.Visibility = Visibility.Collapsed;
                failureText.Visibility = Visibility.Visible;
            };

            var bitmap = new BitmapImage();
            image.Source = bitmap;
            bitmap.UriSource = new Uri(verse.ImageUrl);

            var container = new Grid
            {
                MinWidth = 500,
                MinHeight = 500
            };
            container.Children.Add(image);
            container.Children.Add(loadingRing);
            container.Children.Add(failureText);

            dialog.Content = container;
            await dialog.ShowAsync();
        }
    }
}
