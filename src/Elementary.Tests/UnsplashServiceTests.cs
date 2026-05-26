using Elementary.VerseOfTheDay.Services;
using SkiaSharp;

namespace Elementary.Tests.Services
{
    [TestClass]
    public class UnsplashServiceTests
    {
        [TestMethod]
        public void GenerateFallback_ShouldReturnNonEmptyImage()
        {
            var fallback = UnsplashService.GenerateFallback();

            Assert.IsNotNull(fallback);
            Assert.IsNotNull(fallback.ImageBytes);
            Assert.IsTrue(fallback.ImageBytes!.Length > 0);
            Assert.IsTrue(fallback.IsFallback);
        }

        [TestMethod]
        public void GenerateFallback_ShouldHaveNoAttribution()
        {
            var fallback = UnsplashService.GenerateFallback();

            Assert.IsNull(fallback.PhotographerName);
            Assert.IsNull(fallback.Attribution);
        }

        [TestMethod]
        public void GenerateFallback_ShouldProduceSquareImage()
        {
            var fallback = UnsplashService.GenerateFallback();

            using var codec = SKCodec.Create(new System.IO.MemoryStream(fallback.ImageBytes!));
            Assert.IsNotNull(codec);
            Assert.AreEqual(codec.Info.Width, codec.Info.Height);
        }
    }
}
