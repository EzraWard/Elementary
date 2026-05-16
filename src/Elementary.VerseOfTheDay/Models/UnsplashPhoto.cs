namespace Elementary.VerseOfTheDay.Models
{
    public class UnsplashPhoto
    {
        public byte[]? ImageBytes { get; set; }
        public string? PhotographerName { get; set; }
        public string? PhotographerUrl { get; set; }
        public string? PhotoUrl { get; set; }

        public bool IsFallback { get; set; }

        public string? Attribution =>
            PhotographerName != null
                ? $"Photo by {PhotographerName} on Unsplash"
                : null;
    }
}
