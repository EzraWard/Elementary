using Elementary.VerseOfTheDay.Services;

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
    }
}
