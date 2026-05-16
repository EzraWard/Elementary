using Elementary.VerseOfTheDay.Services;
using System;

namespace Elementary.Tests.Services
{
    [TestClass]
    public class VerseOfTheDayServiceTests
    {
        [TestMethod]
        public void NormalizeWhitespace_ShouldCollapseMultipleSpaces()
        {
            var result = VerseFetchService.NormalizeWhitespace("For  God   so  loved");
            Assert.AreEqual("For God so loved", result);
        }

        [TestMethod]
        public void NormalizeWhitespace_ShouldStripHtmlTags()
        {
            var result = VerseFetchService.NormalizeWhitespace("For <b>God</b> so loved");
            Assert.AreEqual("For God so loved", result);
        }

        [TestMethod]
        public void NormalizeWhitespace_ShouldTrimLeadingAndTrailingWhitespace()
        {
            var result = VerseFetchService.NormalizeWhitespace("  For God so loved  ");
            Assert.AreEqual("For God so loved", result);
        }

        [TestMethod]
        public void NormalizeWhitespace_ShouldReturnEmptyStringUnchanged()
        {
            var result = VerseFetchService.NormalizeWhitespace(string.Empty);
            Assert.AreEqual(string.Empty, result);
        }
    }
}
