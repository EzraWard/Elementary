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
        private readonly IVotdImageCompositor _compositor;
        private readonly IVotdCacheService _cache;

        public VerseOfTheDayService(
            IVerseFetchService verseFetch,
            IVotdImageCompositor compositor,
            IVotdCacheService cache)
        {
            _verseFetch = verseFetch ?? throw new ArgumentNullException(nameof(verseFetch));
            _compositor = compositor ?? throw new ArgumentNullException(nameof(compositor));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task<VerseOfTheDayResult> GetAsync(VotdImageSize size)
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var cacheKey = $"{today}_abstract-v1_{size}";
            Debug.WriteLine($"[VerseOfTheDayService] Requesting VOTD for size={size}, cacheKey='{cacheKey}'.");

            return await _cache.GetOrFetchAsync(cacheKey, () => FetchAndComposeAsync(size, today))
                                .ConfigureAwait(false);
        }

        public void InvalidateToday()
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            Debug.WriteLine($"[VerseOfTheDayService] Invalidating today's VOTD cache for {today}.");
            _cache.Invalidate($"{today}_verse");
            _cache.Invalidate($"{today}_abstract-v1_{VotdImageSize.Widget640x360}");
            _cache.Invalidate($"{today}_abstract-v1_{VotdImageSize.TileMedium150}");
            _cache.Invalidate($"{today}_abstract-v1_{VotdImageSize.TileWide310x150}");
            _cache.Invalidate($"{today}_abstract-v1_{VotdImageSize.TileLarge310x310}");
            _cache.Invalidate($"{today}_abstract-v1_{VotdImageSize.InApp}");
            // Remove keys produced by the previous photo-backed compositor too.
            _cache.Invalidate($"{today}_photo");
            _cache.Invalidate($"{today}_{VotdImageSize.Widget640x360}");
            _cache.Invalidate($"{today}_{VotdImageSize.TileMedium150}");
            _cache.Invalidate($"{today}_{VotdImageSize.TileWide310x150}");
            _cache.Invalidate($"{today}_{VotdImageSize.TileLarge310x310}");
            _cache.Invalidate($"{today}_{VotdImageSize.InApp}");
        }

        private async Task<VerseOfTheDayResult> FetchAndComposeAsync(VotdImageSize size, string dateKey)
        {
            Debug.WriteLine($"[VerseOfTheDayService] Fetching verse for '{dateKey}'.");
            var verse = await _cache.GetOrFetchAsync($"{dateKey}_verse", () => _verseFetch.FetchAsync())
                                    .ConfigureAwait(false);
            Debug.WriteLine($"[VerseOfTheDayService] Verse fetched. Reference='{verse.Reference}'.");

            var imageBytes = _compositor.Compose(verse, size, dateKey);
            Debug.WriteLine($"[VerseOfTheDayService] Composited {imageBytes.Length} bytes for size={size}.");

            return new VerseOfTheDayResult
            {
                VerseText = verse.VerseText,
                Reference = verse.Reference,
                ImageBytes = imageBytes,
                RetrievedAt = DateTime.UtcNow
            };
        }
    }
}
