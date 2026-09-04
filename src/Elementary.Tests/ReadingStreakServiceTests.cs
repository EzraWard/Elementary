using Elementary.Core.Interfaces;
using Elementary.Core.Models;
using Elementary.Core.Services;
using Moq;
using System;
using System.Collections.Generic;

namespace Elementary.Tests.Services
{
    [TestClass]
    public class ReadingStreakServiceTests
    {
        private Mock<ISettingsService> _settingsServiceMock = null!;
        private ReadingStreakProgress _storedProgress = null!;
        private ReadingStreakService _readingStreakService = null!;

        [TestInitialize]
        public void Setup()
        {
            _storedProgress = new ReadingStreakProgress();
            _settingsServiceMock = new Mock<ISettingsService>();
            _settingsServiceMock
                .Setup(x => x.GetReadingStreakProgress())
                .Returns(() => new ReadingStreakProgress
                {
                    ActiveDates = new List<DateTime>(_storedProgress.ActiveDates),
                    DailyReadingSeconds = new Dictionary<DateTime, int>(_storedProgress.DailyReadingSeconds)
                });
            _settingsServiceMock
                .Setup(x => x.SaveReadingStreakProgress(It.IsAny<ReadingStreakProgress>()))
                .Callback<ReadingStreakProgress>(progress =>
                {
                    _storedProgress = new ReadingStreakProgress
                    {
                        ActiveDates = new List<DateTime>(progress?.ActiveDates ?? new List<DateTime>()),
                        DailyReadingSeconds = new Dictionary<DateTime, int>(progress?.DailyReadingSeconds ?? new Dictionary<DateTime, int>())
                    };
                });

            _readingStreakService = new ReadingStreakService(_settingsServiceMock.Object);
        }

        [TestMethod]
        public void GetDailyThreshold_ShouldRequireTenMinutes()
        {
            Assert.AreEqual(TimeSpan.FromMinutes(10), _readingStreakService.GetDailyThreshold());
        }

        [TestMethod]
        public void AddReadingTime_ShouldNotActivateStreakBeforeThreshold()
        {
            var today = new DateTime(2026, 5, 28);
            var threshold = _readingStreakService.GetDailyThreshold();

            _readingStreakService.AddReadingTime(threshold - TimeSpan.FromSeconds(1), today);

            Assert.AreEqual(0, _storedProgress.ActiveDates.Count);
            Assert.AreEqual((int)(threshold - TimeSpan.FromSeconds(1)).TotalSeconds, _storedProgress.DailyReadingSeconds[today]);
        }

        [TestMethod]
        public void AddReadingTime_ShouldActivateStreakAtThresholdCumulatively()
        {
            var today = new DateTime(2026, 5, 28);
            var threshold = _readingStreakService.GetDailyThreshold();

            _readingStreakService.AddReadingTime(TimeSpan.FromSeconds(Math.Floor(threshold.TotalSeconds / 2d)), today);
            _readingStreakService.AddReadingTime(threshold - TimeSpan.FromSeconds(Math.Floor(threshold.TotalSeconds / 2d)), today);

            Assert.AreEqual(1, _storedProgress.ActiveDates.Count);
            Assert.AreEqual(today, _storedProgress.ActiveDates[0]);
            Assert.AreEqual((int)threshold.TotalSeconds, _storedProgress.DailyReadingSeconds[today]);
        }

        [TestMethod]
        public void GetCurrentStreak_ShouldCountConsecutiveDatesEndingToday()
        {
            _storedProgress.ActiveDates = new List<DateTime>
            {
                DateTime.Today.AddDays(-2),
                DateTime.Today.AddDays(-1),
                DateTime.Today
            };

            var currentStreak = _readingStreakService.GetCurrentStreak();

            Assert.AreEqual(3, currentStreak);
        }

        [TestMethod]
        public void GetCurrentStreak_ShouldReturnZeroAfterGapLongerThanOneDay()
        {
            _storedProgress.ActiveDates = new List<DateTime>
            {
                DateTime.Today.AddDays(-3),
                DateTime.Today.AddDays(-2)
            };

            var currentStreak = _readingStreakService.GetCurrentStreak();

            Assert.AreEqual(0, currentStreak);
        }

        [TestMethod]
        public void GetLongestStreak_ShouldReturnLongestRun()
        {
            _storedProgress.ActiveDates = new List<DateTime>
            {
                DateTime.Today.AddDays(-10),
                DateTime.Today.AddDays(-9),
                DateTime.Today.AddDays(-8),
                DateTime.Today.AddDays(-5),
                DateTime.Today.AddDays(-4)
            };

            var longestStreak = _readingStreakService.GetLongestStreak();

            Assert.AreEqual(3, longestStreak);
        }

        [TestMethod]
        public void GetRecentActivity_ShouldCoverRequestedWindow()
        {
            _storedProgress.ActiveDates = new List<DateTime>
            {
                DateTime.Today.AddDays(-1),
                DateTime.Today
            };

            var activity = _readingStreakService.GetRecentActivity(7);

            Assert.AreEqual(7, activity.Count);
            Assert.IsTrue(activity[5]);
            Assert.IsTrue(activity[6]);
        }
    }
}
