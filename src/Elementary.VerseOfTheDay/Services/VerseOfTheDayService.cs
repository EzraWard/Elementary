using Elementary.VerseOfTheDay.Interfaces;
using Elementary.VerseOfTheDay.Models;
using System;
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

            return await _cache.GetOrFetchAsync(cacheKey, () => FetchAndComposeAsync(size, today))
                                .ConfigureAwait(false);
        }

        private async Task<VerseOfTheDayResult> FetchAndComposeAsync(VotdImageSize size, string dateKey)
        {
            // Fetch verse and background image concurrently
            var verseTask = _cache.GetOrFetchAsync($"{dateKey}_verse", () => _verseFetch.FetchAsync());
            var photoTask = _cache.GetOrFetchAsync($"{dateKey}_photo", () => _unsplash.FetchAsync());

            await Task.WhenAll(verseTask, photoTask).ConfigureAwait(false);

            var verse = await verseTask;
            var photo = await photoTask;

            var imageBytes = _compositor.Compose(photo, verse, size);

            return new VerseOfTheDayResult
            {
                VerseText = verse.VerseText,
                Reference = verse.Reference,
                ImageBytes = imageBytes,
                UnsplashAttribution = photo.Attribution,
                RetrievedAt = DateTime.UtcNow
            };
        }
    }
}
