using Elementary.Core.Enums;
using Elementary.Core.Interfaces;
using Elementary.Core.Models;
using Elementary.Core.Services;
using Moq;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Elementary.Tests.Services
{
    [TestClass]
    public class SearchServiceTests
    {
        private Mock<IBibleService> _bibleServiceMock;
        private SearchService _searchService;

        [TestInitialize]
        public void Setup()
        {
            _bibleServiceMock = new Mock<IBibleService>();
            _bibleServiceMock
                .Setup(x => x.EnsureBookLoaded(It.IsAny<ETranslation>(), It.IsAny<Book>()))
                .Returns(Task.CompletedTask);
            _searchService = new SearchService(_bibleServiceMock.Object);
        }

        private static Bible CreateTestBible()
        {
            var bible = new Bible();
            bible.Books.Add(CreateBook("Genesis", new[]
            {
                CreateChapter(1, new[] { (1, "In the beginning God created the heavens and the earth."), (2, "The earth was formless and empty.") }),
                CreateChapter(2, new[] { (1, "Thus the heavens and the earth were completed.") })
            }));
            bible.Books.Add(CreateBook("Matthew", new[]
            {
                CreateChapter(1, new[] { (1, "The book of the genealogy of Jesus Christ."), (23, "and they shall call his name Immanuel, which means God with us.") })
            }));
            bible.Books.Add(CreateBook("Revelation", new[]
            {
                CreateChapter(1, new[] { (1, "The revelation of Jesus Christ which God gave him.") })
            }));
            return bible;
        }

        private static Book CreateBook(string title, Chapter[] chapters)
        {
            return new Book
            {
                Title = title,
                IsChaptersLoaded = true,
                Chapters = new ObservableCollection<Chapter>(chapters)
            };
        }

        private static Chapter CreateChapter(int index, (int verseNum, string text)[] verses)
        {
            var chapter = new Chapter
            {
                Index = index,
                DisplayLines = new ObservableCollection<ChapterDisplayLine>()
            };
            foreach (var (verseNum, text) in verses)
            {
                chapter.DisplayLines.Add(new ChapterDisplayLine
                {
                    Type = ChapterDisplayLineType.Verse,
                    VerseNumber = verseNum,
                    Text = text
                });
            }
            return chapter;
        }

        [TestMethod]
        public async Task SearchAsync_EmptyQuery_ReturnsNoResults()
        {
            var bible = CreateTestBible();
            var results = await _searchService.SearchAsync(bible, ETranslation.NET, "", ESearchScope.EntireBible);
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public async Task SearchAsync_NullQuery_ReturnsNoResults()
        {
            var bible = CreateTestBible();
            var results = await _searchService.SearchAsync(bible, ETranslation.NET, null, ESearchScope.EntireBible);
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public async Task SearchAsync_MatchFound_ReturnsCorrectResults()
        {
            var bible = CreateTestBible();
            var results = await _searchService.SearchAsync(bible, ETranslation.NET, "beginning", ESearchScope.EntireBible);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Genesis", results[0].BookTitle);
            Assert.AreEqual(1, results[0].ChapterIndex);
            Assert.AreEqual(1, results[0].VerseNumber);
        }

        [TestMethod]
        public async Task SearchAsync_CaseInsensitive_MatchesRegardlessOfCase()
        {
            var bible = CreateTestBible();
            var results = await _searchService.SearchAsync(bible, ETranslation.NET, "BEGINNING", ESearchScope.EntireBible);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Genesis", results[0].BookTitle);
        }

        [TestMethod]
        public async Task SearchAsync_MultipleMatches_ReturnsAll()
        {
            var bible = CreateTestBible();
            var results = await _searchService.SearchAsync(bible, ETranslation.NET, "God", ESearchScope.EntireBible);

            Assert.IsTrue(results.Count >= 3, $"Expected at least 3 results but got {results.Count}");
        }

        [TestMethod]
        public async Task SearchAsync_OldTestamentScope_ExcludesNewTestament()
        {
            var bible = CreateTestBible();
            var results = await _searchService.SearchAsync(bible, ETranslation.NET, "Jesus", ESearchScope.OldTestament);

            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public async Task SearchAsync_NewTestamentScope_ExcludesOldTestament()
        {
            var bible = CreateTestBible();
            var results = await _searchService.SearchAsync(bible, ETranslation.NET, "formless", ESearchScope.NewTestament);

            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public async Task SearchAsync_NewTestamentScope_IncludesNewTestamentBooks()
        {
            var bible = CreateTestBible();
            var results = await _searchService.SearchAsync(bible, ETranslation.NET, "Jesus", ESearchScope.NewTestament);

            Assert.IsTrue(results.Count >= 2);
            Assert.IsTrue(results.All(r => r.BookTitle == "Matthew" || r.BookTitle == "Revelation"));
        }

        [TestMethod]
        public async Task SearchAsync_EntireBible_SearchesAllBooks()
        {
            var bible = CreateTestBible();
            var results = await _searchService.SearchAsync(bible, ETranslation.NET, "earth", ESearchScope.EntireBible);

            Assert.AreEqual(3, results.Count);
        }

        [TestMethod]
        public async Task SearchAsync_ResultHasCorrectReferenceText()
        {
            var bible = CreateTestBible();
            var results = await _searchService.SearchAsync(bible, ETranslation.NET, "genealogy", ESearchScope.EntireBible);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Matthew 1:1", results[0].ReferenceText);
        }

        [TestMethod]
        public async Task SearchAsync_NoMatch_ReturnsEmpty()
        {
            var bible = CreateTestBible();
            var results = await _searchService.SearchAsync(bible, ETranslation.NET, "xyznonexistent", ESearchScope.EntireBible);

            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public async Task SearchAsync_EnsureBookLoaded_CalledForEachBook()
        {
            var bible = CreateTestBible();
            await _searchService.SearchAsync(bible, ETranslation.NET, "anything", ESearchScope.EntireBible);

            _bibleServiceMock.Verify(
                x => x.EnsureBookLoaded(ETranslation.NET, It.IsAny<Book>()),
                Times.Exactly(bible.Books.Count));
        }
    }
}
