using Elementary.Core.Services;

namespace Elementary.Tests.Services
{
    [TestClass]
    public class ReadingSessionEligibilityTests
    {
        [TestMethod]
        public void ShouldCount_ShouldReturnTrueOnlyForAnActiveVisibleReader()
        {
            Assert.IsTrue(ReadingSessionEligibility.ShouldCount(
                isReaderLoaded: true,
                isReaderPageActive: true,
                isWindowVisible: true,
                isWindowActive: true,
                isReaderObscured: false,
                isApplicationSuspended: false));
        }

        [TestMethod]
        [DataRow(false, true, true, true, false, false)]
        [DataRow(true, false, true, true, false, false)]
        [DataRow(true, true, false, true, false, false)]
        [DataRow(true, true, true, false, false, false)]
        [DataRow(true, true, true, true, true, false)]
        [DataRow(true, true, true, true, false, true)]
        public void ShouldCount_ShouldReturnFalseWhenAnyEligibilityConditionFails(
            bool isReaderLoaded,
            bool isReaderPageActive,
            bool isWindowVisible,
            bool isWindowActive,
            bool isReaderObscured,
            bool isApplicationSuspended)
        {
            Assert.IsFalse(ReadingSessionEligibility.ShouldCount(
                isReaderLoaded,
                isReaderPageActive,
                isWindowVisible,
                isWindowActive,
                isReaderObscured,
                isApplicationSuspended));
        }
    }
}
