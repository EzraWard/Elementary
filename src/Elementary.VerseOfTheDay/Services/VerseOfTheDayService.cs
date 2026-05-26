using Elementary.VerseOfTheDay.Interfaces;
using Elementary.VerseOfTheDay.Models;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Elementary.VerseOfTheDay.Services
{
    public class VerseOfTheDayService : IVerseOfTheDayService
    {
        private readonly IVerseFetchService _verseFetch;
        private readonly IUnsplashService _unsplash;
        private readonly IVotdImageCompositor _compositor;
        private readonly IVotdCacheService _cache;

        public VerseOfTheDayService(
            IVerseFetchService verseFetch,
            IUnsplashService unsplash,
            IVotdImageCompositor compositor,
            IVotdCacheService cache)
        {
            _verseFetch = verseFetch ?? throw new ArgumentNullException(nameof(verseFetch));
            _unsplash = unsplash ?? throw new ArgumentNullException(nameof(unsplash));
            _compositor = compositor ?? throw new ArgumentNullException(nameof(compositor));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task<VerseOfTheDayResult> GetAsync(VotdImageSize size)
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var cacheKey = $"{today}_{size}";
            Debug.WriteLine($"[VerseOfTheDayService] Requesting VOTD for size={size}, cacheKey='{cacheKey}'.");

            return await _cache.GetOrFetchAsync(cacheKey, () => FetchAndComposeAsync(size, today))
                                .ConfigureAwait(false);
        }

        public void InvalidateToday()
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            Debug.WriteLine($"[VerseOfTheDayService] Invalidating today's VOTD cache for {today}.");
            _cache.Invalidate($"{today}_verse");
            _cache.Invalidate($"{today}_photo");
            _cache.Invalidate($"{today}_{VotdImageSize.Widget640x360}");
            _cache.Invalidate($"{today}_{VotdImageSize.TileMedium150}");
            _cache.Invalidate($"{today}_{VotdImageSize.TileWide310x150}");
            _cache.Invalidate($"{today}_{VotdImageSize.TileLarge310x310}");
            _cache.Invalidate($"{today}_{VotdImageSize.InApp}");
        }

        private async Task<VerseOfTheDayResult> FetchAndComposeAsync(VotdImageSize size, string dateKey)
        {
            Debug.WriteLine($"[VerseOfTheDayService] Fetching verse and photo for '{dateKey}'.");
            // Fetch verse and background image concurrently
            var verseTask = _cache.GetOrFetchAsync($"{dateKey}_verse", () => _verseFetch.FetchAsync());
            var photoTask = _cache.GetOrFetchAsync($"{dateKey}_photo", () => _unsplash.FetchAsync());

            await Task.WhenAll(verseTask, photoTask).ConfigureAwait(false);

            var verse = await verseTask;
            var photo = await photoTask;
            Debug.WriteLine($"[VerseOfTheDayService] Verse fetched. Reference='{verse.Reference}'. Photo fallback={photo.IsFallback} attribution='{photo.Attribution ?? "<none>"}'.");

            var imageBytes = _compositor.Compose(photo, verse, size);
            Debug.WriteLine($"[VerseOfTheDayService] Composited {imageBytes.Length} bytes for size={size}.");

            return new VerseOfTheDayResult
            {
                VerseText = verse.VerseText,
                Reference = verse.Reference,
                ImageBytes = imageBytes,
                UnsplashAttribution = photo.Attribution,
                UsedFallbackBackground = photo.IsFallback,
                RetrievedAt = DateTime.UtcNow
            };
        }
    }
}
