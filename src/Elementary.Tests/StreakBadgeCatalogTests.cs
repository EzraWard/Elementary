using Elementary.Core.Models;
using System.Linq;

namespace Elementary.Tests.Models
{
    [TestClass]
    public class StreakBadgeCatalogTests
    {
        [TestMethod]
        public void BuildProgress_ShouldMarkFirstBadgeAsNextWhenNoStreakExists()
        {
            var badges = StreakBadgeCatalog.BuildProgress(0).ToList();

            Assert.AreEqual(6, badges.Count);
            Assert.IsFalse(badges[0].IsEarned);
            Assert.IsTrue(badges[0].IsNextToEarn);
            Assert.AreEqual("Next up at 1 day", badges[0].StatusText);
            Assert.AreEqual(1, badges.Count(badge => badge.IsNextToEarn));
            Assert.IsTrue(badges.Skip(1).All(badge => !badge.IsEarned && !badge.IsNextToEarn));
        }

        [TestMethod]
        public void BuildProgress_ShouldUnlockFirstDayBadgeAfterOneDay()
        {
            var badges = StreakBadgeCatalog.BuildProgress(1).ToList();

            Assert.IsTrue(badges[0].IsEarned);
            Assert.IsFalse(badges[0].IsNextToEarn);
            Assert.AreEqual("Unlocked", badges[0].StatusText);
            Assert.IsFalse(badges[1].IsEarned);
            Assert.IsTrue(badges[1].IsNextToEarn);
        }

        [TestMethod]
        public void BuildProgress_ShouldMarkTwoWeekBadgeAsNextAfterSevenDays()
        {
            var badges = StreakBadgeCatalog.BuildProgress(7).ToList();

            Assert.IsTrue(badges[0].IsEarned);
            Assert.IsTrue(badges[1].IsEarned);
            Assert.IsFalse(badges[2].IsEarned);
            Assert.IsTrue(badges[2].IsNextToEarn);
            Assert.AreEqual("Two weeks", badges[2].Title);
            Assert.AreEqual(14, badges[2].ThresholdDays);
        }

        [TestMethod]
        public void BuildProgress_ShouldAdvanceToThirtyDayBadgeAfterFourteenDays()
        {
            var badges = StreakBadgeCatalog.BuildProgress(14).ToList();

            Assert.IsTrue(badges[2].IsEarned);
            Assert.IsFalse(badges[3].IsEarned);
            Assert.IsTrue(badges[3].IsNextToEarn);
            Assert.AreEqual("One month", badges[3].Title);
            Assert.AreEqual(30, badges[3].ThresholdDays);
        }

        [TestMethod]
        public void BuildProgress_ShouldUnlockAllCurrentBadgesAtThreeHundredSixtyFiveDays()
        {
            var badges = StreakBadgeCatalog.BuildProgress(365).ToList();

            Assert.IsTrue(badges.All(badge => badge.IsEarned));
            Assert.IsTrue(badges.All(badge => !badge.IsNextToEarn));
        }
    }
}
