using Elementary.VerseOfTheDay.Interfaces;
using Elementary.VerseOfTheDay.Models;
using SkiaSharp;
using System;
using System.Diagnostics;
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
            if (!HasConfiguredAccessKey())
            {
                Debug.WriteLine("[UnsplashService] Unsplash access key is not configured. Using fallback background.");
                return GenerateFallback();
            }

            try
            {
                var metadataRequest = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"{ApiBase}?query=nature&orientation=landscape&content_filter=high");
                metadataRequest.Headers.TryAddWithoutValidation("Accept-Version", "v1");
                metadataRequest.Headers.TryAddWithoutValidation("Authorization", $"Client-ID {ApiKeys.UnsplashAccessKey}");

                Debug.WriteLine($"[UnsplashService] Requesting random photo metadata from '{ApiBase}' using Authorization: Client-ID and Accept-Version: v1.");
                using var metadataResponse = await _httpClient.SendAsync(metadataRequest).ConfigureAwait(false);
                var json = await metadataResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!metadataResponse.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"[UnsplashService] Metadata request failed with {(int)metadataResponse.StatusCode} {metadataResponse.ReasonPhrase}. Body='{Truncate(json, 300)}'. Using fallback background.");
                    return GenerateFallback();
                }

                var meta = ParseMeta(json);

                var imageUrl = BuildOptimizedImageUrl(meta.rawImageUrl ?? meta.imageUrl);
                if (string.IsNullOrEmpty(imageUrl))
                {
                    Debug.WriteLine("[UnsplashService] Metadata response did not include a usable image URL. Using fallback background.");
                    return GenerateFallback();
                }

                Debug.WriteLine($"[UnsplashService] Downloading photo bytes from '{imageUrl}'.");
                using var imageResponse = await _httpClient.GetAsync(imageUrl).ConfigureAwait(false);
                if (!imageResponse.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"[UnsplashService] Image download failed with {(int)imageResponse.StatusCode} {imageResponse.ReasonPhrase}. Using fallback background.");
                    return GenerateFallback();
                }

                var imageBytes = await imageResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

                // Keep only images large enough to cover both wide tiles and square in-app output.
                if (!MeetsMinimumSize(imageBytes, 1080, 1080))
                {
                    Debug.WriteLine("[UnsplashService] Downloaded image is smaller than 1080x1080. Using fallback background.");
                    return GenerateFallback();
                }

                Debug.WriteLine($"[UnsplashService] Downloaded Unsplash image ({imageBytes.Length} bytes) successfully.");

                return new UnsplashPhoto
                {
                    ImageBytes = imageBytes,
                    PhotographerName = meta.photographerName,
                    PhotographerUrl = meta.photographerUrl,
                    PhotoUrl = imageUrl,
                    IsFallback = false
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UnsplashService] Fetch failed: {ex}");
                return GenerateFallback();
            }
        }

        private static bool HasConfiguredAccessKey()
        {
            return !string.IsNullOrWhiteSpace(ApiKeys.UnsplashAccessKey)
                && !string.Equals(ApiKeys.UnsplashAccessKey, "YOUR_UNSPLASH_ACCESS_KEY_HERE", StringComparison.Ordinal);
        }

        private static (string? imageUrl, string? rawImageUrl, string? photographerName, string? photographerUrl) ParseMeta(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string? imageUrl = null;
            string? rawImageUrl = null;
            if (root.TryGetProperty("urls", out var urls) && urls.TryGetProperty("full", out var full))
                imageUrl = full.GetString();
            if (root.TryGetProperty("urls", out urls) && urls.TryGetProperty("raw", out var raw))
                rawImageUrl = raw.GetString();

            string? photographerName = null;
            string? photographerUrl = null;
            if (root.TryGetProperty("user", out var user))
            {
                if (user.TryGetProperty("name", out var nameEl))
                    photographerName = nameEl.GetString();
                if (user.TryGetProperty("links", out var links) && links.TryGetProperty("html", out var htmlEl))
                    photographerUrl = htmlEl.GetString();
            }

            return (imageUrl, rawImageUrl, photographerName, photographerUrl);
        }

        private static string? BuildOptimizedImageUrl(string? baseImageUrl)
        {
            if (string.IsNullOrWhiteSpace(baseImageUrl))
            {
                return baseImageUrl;
            }

            var resolvedBaseImageUrl = baseImageUrl!;
            var separator = resolvedBaseImageUrl.Contains("?") ? "&" : "?";
            return $"{resolvedBaseImageUrl}{separator}w=1600&h=1600&fit=crop&crop=entropy&auto=format&fm=jpg&q=80";
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
            const int height = 1920;
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

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, maxLength);
        }
    }
}
