using Elementary.Core.Models;
using Elementary.Core.Services;

namespace Elementary.Tests.Services
{
    [TestClass]
    public class NavigationHistoryManagerTests
    {
        [TestMethod]
        public void RecordDeparture_ShouldAddDepartedLocationAndExcludeCurrentLocation()
        {
            var departed = Location("Genesis", 1);
            var current = Location("Genesis", 2);

            var history = NavigationHistoryManager.RecordDeparture(
                new[] { current },
                departed,
                current);

            Assert.AreEqual(1, history.Count);
            Assert.IsTrue(NavigationHistoryManager.AreSameLocation(departed, history[0]));
            Assert.IsFalse(history.Any(item => NavigationHistoryManager.AreSameLocation(current, item)));
        }

        [TestMethod]
        public void RecordDeparture_ShouldMoveAnExistingDepartedLocationToMostRecent()
        {
            var departed = Location("Genesis", 1);
            var older = Location("Exodus", 3);

            var history = NavigationHistoryManager.RecordDeparture(
                new[] { departed, older },
                departed,
                Location("Genesis", 2));

            CollectionAssert.AreEqual(
                new[] { "Exodus 3", "Genesis 1" },
                history.Select(item => item.DisplayText).ToArray());
        }

        [TestMethod]
        public void RecordDeparture_WithNoDeparture_ShouldRemoveCurrentLocationFromExistingHistory()
        {
            var current = Location("John", 3);

            var history = NavigationHistoryManager.RecordDeparture(
                new[] { Location("Genesis", 1), current },
                departedLocation: null,
                currentLocation: current);

            Assert.AreEqual(1, history.Count);
            Assert.AreEqual("Genesis 1", history[0].DisplayText);
        }

        [TestMethod]
        public void RecordDeparture_ShouldKeepOnlyTenMostRecentLocations()
        {
            var existing = Enumerable.Range(1, 10)
                .Select(chapter => Location("Genesis", chapter))
                .ToList();

            var history = NavigationHistoryManager.RecordDeparture(
                existing,
                Location("Exodus", 1),
                Location("Exodus", 2));

            Assert.AreEqual(10, history.Count);
            Assert.AreEqual("Genesis 2", history[0].DisplayText);
            Assert.AreEqual("Exodus 1", history[9].DisplayText);
        }

        private static NavigationHistoryItem Location(string book, int chapter)
        {
            return new NavigationHistoryItem
            {
                BookTitle = book,
                BookKey = book,
                Chapter = chapter
            };
        }
    }
}
