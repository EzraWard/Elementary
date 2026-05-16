using Elementary.VerseOfTheDay.Interfaces;
using Elementary.VerseOfTheDay.Models;
using SkiaSharp;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Elementary.VerseOfTheDay.Services
{
    public class UnsplashService : IUnsplashService
    {
        private const string ApiBase = "https://api.unsplash.com/photos/random";

        private readonly HttpClient _httpClient;

        public UnsplashService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<UnsplashPhoto> FetchAsync()
        {
            try
            {
                var url = $"{ApiBase}?query=nature&orientation=landscape&w=1920&h=1080&client_id={ApiKeys.UnsplashClientId}";
                var json = await _httpClient.GetStringAsync(url).ConfigureAwait(false);
                var meta = ParseMeta(json);

                var imageUrl = meta.imageUrl;
                if (string.IsNullOrEmpty(imageUrl))
                    return GenerateFallback();

                var imageBytes = await _httpClient.GetByteArrayAsync(imageUrl).ConfigureAwait(false);

                // Reject images smaller than 1920×1080
                if (!MeetsMinimumSize(imageBytes, 1920, 1080))
                    return GenerateFallback();

                return new UnsplashPhoto
                {
                    ImageBytes = imageBytes,
                    PhotographerName = meta.photographerName,
                    PhotographerUrl = meta.photographerUrl,
                    PhotoUrl = imageUrl,
                    IsFallback = false
                };
            }
            catch (Exception)
            {
                return GenerateFallback();
            }
        }

        private static (string? imageUrl, string? photographerName, string? photographerUrl) ParseMeta(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string? imageUrl = null;
            if (root.TryGetProperty("urls", out var urls) && urls.TryGetProperty("full", out var full))
                imageUrl = full.GetString();

            string? photographerName = null;
            string? photographerUrl = null;
            if (root.TryGetProperty("user", out var user))
            {
                if (user.TryGetProperty("name", out var nameEl))
                    photographerName = nameEl.GetString();
                if (user.TryGetProperty("links", out var links) && links.TryGetProperty("html", out var htmlEl))
                    photographerUrl = htmlEl.GetString();
            }

            return (imageUrl, photographerName, photographerUrl);
        }

        private static bool MeetsMinimumSize(byte[] imageBytes, int minWidth, int minHeight)
        {
            try
            {
                using var codec = SKCodec.Create(new System.IO.MemoryStream(imageBytes));
                if (codec == null) return false;
                var info = codec.Info;
                return info.Width >= minWidth && info.Height >= minHeight;
            }
            catch
            {
                return false;
            }
        }

        public static UnsplashPhoto GenerateFallback()
        {
            const int width = 1920;
            const int height = 1080;
            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            var canvas = surface.Canvas;

            using var paint = new SKPaint();
            paint.Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                new SKPoint(width, height),
                new[] { new SKColor(20, 45, 70), new SKColor(5, 15, 30) },
                null,
                SKShaderTileMode.Clamp);
            canvas.DrawRect(0, 0, width, height, paint);

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 90);

            return new UnsplashPhoto
            {
                ImageBytes = data.ToArray(),
                IsFallback = true
            };
        }
    }
}
