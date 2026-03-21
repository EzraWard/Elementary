using Elementary.Services;

namespace Elementary.Tests.Services
{
    [TestClass]
    public class VerseOfTheDayServiceTests
    {
        [TestMethod]
        public void GetVerseOfTheDay_ShouldUseProvidedDateInUrl()
        {
            // Arrange
            var service = new VerseOfTheDayService();
            var date = new DateTime(2026, 2, 7);

            // Act
            var result = service.GetVerseOfTheDay(date);

            // Assert
            Assert.AreEqual(date, result.Date);
            Assert.AreEqual("https://votd.olivetree.com/02_07_NKJV.jpg", result.ImageUrl);
        }

        [TestMethod]
        public void GetVerseOfTheDay_ShouldBuildTitleFromDate()
        {
            // Arrange
            var service = new VerseOfTheDayService();
            var date = new DateTime(2026, 12, 25);

            // Act
            var result = service.GetVerseOfTheDay(date);

            // Assert
            Assert.AreEqual($"Verse of the Day for {date.ToShortDateString()}", result.Title);
        }
    }
}
