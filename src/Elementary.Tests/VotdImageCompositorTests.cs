using Elementary.VerseOfTheDay.Models;
using Elementary.VerseOfTheDay.Services;

namespace Elementary.Tests.Services
{
    [TestClass]
    public class VotdImageCompositorTests
    {
        private static readonly BibleVerseData SampleVerse = new BibleVerseData
        {
            VerseText = "For God so loved the world, that he gave his only begotten Son.",
            Book = "John",
            Chapter = "3",
            Verse = "16"
        };

        [TestMethod]
        public void Compose_Widget_ShouldReturnNonEmptyBytes()
        {
            var compositor = new VotdImageCompositor();
            var photo = UnsplashService.GenerateFallback();

            var result = compositor.Compose(photo, SampleVerse, VotdImageSize.Widget640x360);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length > 0);
        }

        [TestMethod]
        public void Compose_TileMedium_ShouldReturnNonEmptyBytes()
        {
            var compositor = new VotdImageCompositor();
            var photo = UnsplashService.GenerateFallback();

            var result = compositor.Compose(photo, SampleVerse, VotdImageSize.TileMedium150);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length > 0);
        }

        [TestMethod]
        public void Compose_TileWide_ShouldReturnNonEmptyBytes()
        {
            var compositor = new VotdImageCompositor();
            var photo = UnsplashService.GenerateFallback();

            var result = compositor.Compose(photo, SampleVerse, VotdImageSize.TileWide310x150);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length > 0);
        }

        [TestMethod]
        public void Compose_TileLarge_ShouldReturnNonEmptyBytes()
        {
            var compositor = new VotdImageCompositor();
            var photo = UnsplashService.GenerateFallback();

            var result = compositor.Compose(photo, SampleVerse, VotdImageSize.TileLarge310x310);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length > 0);
        }

        [TestMethod]
        public void Compose_InApp_ShouldReturnNonEmptyBytes()
        {
            var compositor = new VotdImageCompositor();
            var photo = UnsplashService.GenerateFallback();

            var result = compositor.Compose(photo, SampleVerse, VotdImageSize.InApp);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length > 0);
        }

        [TestMethod]
        public void Compose_WithNullImageBytes_ShouldStillReturnValidImage()
        {
            var compositor = new VotdImageCompositor();
            var emptyPhoto = new UnsplashPhoto { ImageBytes = null, IsFallback = true };

            var result = compositor.Compose(emptyPhoto, SampleVerse, VotdImageSize.InApp);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length > 0);
        }

        [TestMethod]
        public void Compose_AllSizes_ShouldCompleteWithinTimeLimit()
        {
            var compositor = new VotdImageCompositor();
            var photo = UnsplashService.GenerateFallback();

            foreach (VotdImageSize size in System.Enum.GetValues(typeof(VotdImageSize)))
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                compositor.Compose(photo, SampleVerse, size);
                sw.Stop();
                Assert.IsTrue(sw.ElapsedMilliseconds < 500,
                    $"Compose({size}) took {sw.ElapsedMilliseconds}ms (limit: 500ms)");
            }
        }
    }
}
