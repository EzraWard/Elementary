using Elementary.VerseOfTheDay.Services;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Elementary.Tests.Services
{
    [TestClass]
    public class VerseFetchServiceTests
    {
        private static HttpClient BuildMockHttpClient(string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var handler = new MockHttpMessageHandler(responseBody, statusCode);
            return new HttpClient(handler);
        }

        [TestMethod]
        public async Task FetchAsync_ShouldParseVerseText_WhenApiReturnsValidJson()
        {
            const string json = @"[{""bookname"":""John"",""chapter"":""3"",""verse"":""16"",""text"":""For God so loved the world...""}]";
            var service = new VerseFetchService(BuildMockHttpClient(json));

            var result = await service.FetchAsync();

            Assert.AreEqual("For God so loved the world...", result.VerseText);
            Assert.AreEqual("John", result.Book);
            Assert.AreEqual("3", result.Chapter);
            Assert.AreEqual("16", result.Verse);
        }

        [TestMethod]
        public async Task FetchAsync_ShouldReturnDefaultVerse_WhenApiThrows()
        {
            var service = new VerseFetchService(BuildMockHttpClient(string.Empty, HttpStatusCode.InternalServerError));

            var result = await service.FetchAsync();

            Assert.IsFalse(string.IsNullOrEmpty(result.VerseText));
            Assert.IsFalse(string.IsNullOrEmpty(result.Book));
        }

        [TestMethod]
        public async Task FetchAsync_ShouldNormalizeHtmlInVerseText()
        {
            const string json = @"[{""bookname"":""Ps"",""chapter"":""23"",""verse"":""1"",""text"":""The <i>Lord</i> is my shepherd...""}]";
            var service = new VerseFetchService(BuildMockHttpClient(json));

            var result = await service.FetchAsync();

            Assert.IsFalse(result.VerseText.Contains("<i>"));
            Assert.IsTrue(result.VerseText.Contains("Lord"));
        }

        [TestMethod]
        public void Reference_ShouldCombineBookChapterVerse()
        {
            var verse = new Elementary.VerseOfTheDay.Models.BibleVerseData
            {
                Book = "John",
                Chapter = "3",
                Verse = "16"
            };

            Assert.AreEqual("John 3:16", verse.Reference);
        }

        private class MockHttpMessageHandler : HttpMessageHandler
        {
            private readonly string _responseBody;
            private readonly HttpStatusCode _statusCode;

            public MockHttpMessageHandler(string responseBody, HttpStatusCode statusCode)
            {
                _responseBody = responseBody;
                _statusCode = statusCode;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage(_statusCode)
                {
                    Content = new StringContent(_responseBody)
                };
                return Task.FromResult(response);
            }
        }
    }
}
