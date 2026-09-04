using Elementary.VerseOfTheDay.Models;
using Elementary.VerseOfTheDay.Services;
using SkiaSharp;

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

        private const string SampleSeed = "2026-08-14";

        [TestMethod]
        public void Compose_Widget_ShouldReturnNonEmptyBytes()
        {
            var compositor = new VotdImageCompositor();

            var result = compositor.Compose(SampleVerse, VotdImageSize.Widget640x360, SampleSeed);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length > 0);
        }

        [TestMethod]
        public void Compose_TileMedium_ShouldReturnNonEmptyBytes()
        {
            var compositor = new VotdImageCompositor();

            var result = compositor.Compose(SampleVerse, VotdImageSize.TileMedium150, SampleSeed);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length > 0);
        }

        [TestMethod]
        public void Compose_TileWide_ShouldReturnNonEmptyBytes()
        {
            var compositor = new VotdImageCompositor();

            var result = compositor.Compose(SampleVerse, VotdImageSize.TileWide310x150, SampleSeed);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length > 0);
        }

        [TestMethod]
        public void Compose_TileLarge_ShouldReturnNonEmptyBytes()
        {
            var compositor = new VotdImageCompositor();

            var result = compositor.Compose(SampleVerse, VotdImageSize.TileLarge310x310, SampleSeed);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length > 0);
        }

        [TestMethod]
        public void Compose_InApp_ShouldReturnNonEmptyBytes()
        {
            var compositor = new VotdImageCompositor();

            var result = compositor.Compose(SampleVerse, VotdImageSize.InApp, SampleSeed);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length > 0);
        }

        [TestMethod]
        public void Compose_InApp_ShouldReturnSquareImage()
        {
            var compositor = new VotdImageCompositor();

            var result = compositor.Compose(SampleVerse, VotdImageSize.InApp, SampleSeed);

            using var codec = SKCodec.Create(new System.IO.MemoryStream(result));
            Assert.IsNotNull(codec);
            Assert.AreEqual(800, codec.Info.Width);
            Assert.AreEqual(800, codec.Info.Height);
        }

        [TestMethod]
        public void CalculateFontSize_ShouldReturnLargerFontForShorterVerse()
        {
            const float textAreaWidth = 600f;
            const float textAreaHeight = 420f;
            const int imageWidth = 800;

            var shortFontSize = VotdImageCompositor.CalculateFontSize(
                "Jesus wept.",
                textAreaWidth,
                textAreaHeight,
                imageWidth);

            var longFontSize = VotdImageCompositor.CalculateFontSize(
                "Blessed be the God and Father of our Lord Jesus Christ, who has blessed us with every spiritual blessing in the heavenly realms in Christ.",
                textAreaWidth,
                textAreaHeight,
                imageWidth);

            Assert.IsTrue(shortFontSize > longFontSize);
        }

        [TestMethod]
        public void Compose_WithSameSeed_ShouldReturnIdenticalArtwork()
        {
            var compositor = new VotdImageCompositor();

            var first = compositor.Compose(SampleVerse, VotdImageSize.InApp, SampleSeed);
            var second = compositor.Compose(SampleVerse, VotdImageSize.InApp, SampleSeed);

            CollectionAssert.AreEqual(first, second);
        }

        [TestMethod]
        public void Compose_WithDifferentSeed_ShouldReturnDifferentArtwork()
        {
            var compositor = new VotdImageCompositor();

            var first = compositor.Compose(SampleVerse, VotdImageSize.InApp, SampleSeed);
            var second = compositor.Compose(SampleVerse, VotdImageSize.InApp, "2026-08-15");

            CollectionAssert.AreNotEqual(first, second);
        }

        [TestMethod]
        public void Compose_AllSizes_ShouldCompleteWithinTimeLimit()
        {
            var compositor = new VotdImageCompositor();

            foreach (VotdImageSize size in System.Enum.GetValues(typeof(VotdImageSize)))
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                compositor.Compose(SampleVerse, size, SampleSeed);
                sw.Stop();
                Assert.IsTrue(sw.ElapsedMilliseconds < 500,
                    $"Compose({size}) took {sw.ElapsedMilliseconds}ms (limit: 500ms)");
            }
        }
    }
}
