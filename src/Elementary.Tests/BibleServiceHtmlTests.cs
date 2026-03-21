using Elementary.Core.Services;

namespace Elementary.Tests.Services
{
    [TestClass]
    public class BibleServiceHtmlTests
    {
        [TestMethod]
        public void CleanVerseMarkers_ShouldConvertVerseSpansToSupVerseNumbers()
        {
            // Arrange
            var html = "<p><span class=\"verse\">3:16</span> For God so loved the world.</p>";

            // Act
            var cleaned = BibleService.CleanVerseMarkers(html);

            // Assert
            Assert.AreEqual("<p><sup>16</sup> For God so loved the world.</p>", cleaned);
        }

        [TestMethod]
        public void RemoveVerseMarkers_ShouldRemoveVerseSpansCompletely()
        {
            // Arrange
            var html = "<p><span class=\"verse\">1:1</span> In the beginning.</p>";

            // Act
            var cleaned = BibleService.RemoveVerseMarkers(html);

            // Assert
            Assert.AreEqual("<p> In the beginning.</p>", cleaned);
            Assert.IsFalse(cleaned.Contains("class=\"verse\""));
        }
    }
}
