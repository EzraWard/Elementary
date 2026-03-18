using Elementary.Models;
using Elementary.Widgets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Elementary.Tests.Widgets
{
    [TestClass]
    public class WidgetDataBuilderTests
    {
        [TestMethod]
        public void BuildAdaptiveCardData_ShouldIncludeImageUrl()
        {
            var verse = new VerseOfTheDay
            {
                Date = new DateTime(2026, 3, 18),
                ImageUrl = "https://votd.olivetree.com/03_18_NKJV.jpg"
            };

            var result = WidgetDataBuilder.BuildAdaptiveCardData(verse);

            StringAssert.Contains(result, "\"imageUrl\":\"https://votd.olivetree.com/03_18_NKJV.jpg\"");
        }

        [TestMethod]
        public void BuildAdaptiveCardData_ShouldIncludeTitle()
        {
            var date = new DateTime(2026, 3, 18);
            var verse = new VerseOfTheDay
            {
                Date = date,
                ImageUrl = "https://votd.olivetree.com/03_18_NKJV.jpg"
            };

            var result = WidgetDataBuilder.BuildAdaptiveCardData(verse);

            StringAssert.Contains(result, $"\"title\":\"Verse of the Day for {date.ToShortDateString()}\"");
        }

        [TestMethod]
        public void BuildAdaptiveCardData_ShouldProduceValidJson()
        {
            var verse = new VerseOfTheDay
            {
                Date = new DateTime(2026, 12, 25),
                ImageUrl = "https://votd.olivetree.com/12_25_NKJV.jpg"
            };

            var result = WidgetDataBuilder.BuildAdaptiveCardData(verse);

            Assert.IsTrue(result.StartsWith("{") && result.EndsWith("}"),
                "Result should be a JSON object.");
        }

        [TestMethod]
        public void BuildAdaptiveCardData_ShouldEscapeDoubleQuotesInUrl()
        {
            var verse = new VerseOfTheDay
            {
                Date = new DateTime(2026, 1, 1),
                ImageUrl = "https://example.com/image\"with\"quotes.jpg"
            };

            var result = WidgetDataBuilder.BuildAdaptiveCardData(verse);

            // Raw double quotes in the URL must be escaped so the JSON stays valid.
            StringAssert.Contains(result, "\\\"with\\\"quotes");
        }

        [TestMethod]
        public void BuildAdaptiveCardData_ShouldThrowForNullVerse()
        {
            bool threw = false;
            try
            {
                WidgetDataBuilder.BuildAdaptiveCardData(null);
            }
            catch (ArgumentNullException)
            {
                threw = true;
            }
            Assert.IsTrue(threw, "Expected ArgumentNullException for null verse.");
        }

        [TestMethod]
        public void BuildAdaptiveCardData_ShouldHandleNullImageUrl()
        {
            var verse = new VerseOfTheDay
            {
                Date = new DateTime(2026, 3, 18),
                ImageUrl = null
            };

            var result = WidgetDataBuilder.BuildAdaptiveCardData(verse);

            StringAssert.Contains(result, "\"imageUrl\":\"\"");
        }
    }
}
