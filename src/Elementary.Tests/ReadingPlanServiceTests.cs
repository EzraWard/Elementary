using Elementary.Core.Interfaces;
using Elementary.Core.Models;
using Elementary.Core.Services;
using Moq;
using System.Linq;

namespace Elementary.Tests.Services
{
    [TestClass]
    public class ReadingPlanServiceTests
    {
        private Mock<ISettingsService> _settingsServiceMock = null!;
        private ReadingPlanProgress _storedProgress = null!;
        private ReadingPlanService _readingPlanService = null!;

        [TestInitialize]
        public void Setup()
        {
            _storedProgress = new ReadingPlanProgress();
            _settingsServiceMock = new Mock<ISettingsService>();
            _settingsServiceMock
                .Setup(x => x.GetReadingPlanProgress())
                .Returns(() => new ReadingPlanProgress
                {
                    ActivePlanId = _storedProgress.ActivePlanId,
                    CompletedDayCount = _storedProgress.CompletedDayCount
                });
            _settingsServiceMock
                .Setup(x => x.SaveReadingPlanProgress(It.IsAny<ReadingPlanProgress>()))
                .Callback<ReadingPlanProgress>(progress =>
                {
                    _storedProgress = new ReadingPlanProgress
                    {
                        ActivePlanId = progress?.ActivePlanId,
                        CompletedDayCount = progress?.CompletedDayCount ?? 0
                    };
                });

            _readingPlanService = new ReadingPlanService(_settingsServiceMock.Object);
        }

        [TestMethod]
        public void GetBuiltInPlans_ShouldReturnCatalog()
        {
            var plans = _readingPlanService.GetBuiltInPlans();

            Assert.AreEqual(3, plans.Count);
            Assert.IsTrue(plans.Any(plan => plan.Id == "john-in-21-days"));
            Assert.IsTrue(plans.Any(plan => plan.Id == "proverbs-in-31-days"));
            Assert.IsTrue(plans.Any(plan => plan.Id == "new-testament-in-260-days"));
        }

        [TestMethod]
        public void StartPlan_ShouldPersistSelectedPlanWithZeroCompletedDays()
        {
            _readingPlanService.StartPlan("john-in-21-days");

            Assert.AreEqual("john-in-21-days", _storedProgress.ActivePlanId);
            Assert.AreEqual(0, _storedProgress.CompletedDayCount);
        }

        [TestMethod]
        public void GetCurrentDay_ShouldReturnFirstDayWhenPlanJustStarted()
        {
            _readingPlanService.StartPlan("john-in-21-days");

            var currentDay = _readingPlanService.GetCurrentDay();

            Assert.IsNotNull(currentDay);
            Assert.AreEqual(1, currentDay.DayNumber);
            Assert.AreEqual("John 1", currentDay.Summary);
        }

        [TestMethod]
        public void CompleteCurrentDay_ShouldAdvanceProgress()
        {
            _readingPlanService.StartPlan("john-in-21-days");

            var completed = _readingPlanService.CompleteCurrentDay();
            var currentDay = _readingPlanService.GetCurrentDay();

            Assert.IsTrue(completed);
            Assert.AreEqual(1, _storedProgress.CompletedDayCount);
            Assert.IsNotNull(currentDay);
            Assert.AreEqual(2, currentDay.DayNumber);
        }

        [TestMethod]
        public void CompleteCurrentDay_ShouldReturnFalseWhenPlanAlreadyFinished()
        {
            var plan = _readingPlanService.GetBuiltInPlans().First(planDefinition => planDefinition.Id == "john-in-21-days");
            _storedProgress = new ReadingPlanProgress
            {
                ActivePlanId = plan.Id,
                CompletedDayCount = plan.TotalDays
            };

            var completed = _readingPlanService.CompleteCurrentDay();

            Assert.IsFalse(completed);
            Assert.IsNull(_readingPlanService.GetCurrentDay());
        }
    }
}
