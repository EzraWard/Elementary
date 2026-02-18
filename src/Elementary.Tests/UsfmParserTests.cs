using Elementary.Core.Parsers;
using System.Linq;

namespace Elementary.Tests
{
    [TestClass]
    public class UsfmParserTests
    {
        [TestMethod]
        public void ParseBook_ShouldExtractTitleAndChapters()
        {
            var usfm = "\\id TEST\\n\\h TestBook\\n\\c 1\\n\\v 1 In the beginning.\\n\\v 2 The second verse.\\n\\c 2\\n\\v 1 New chapter verse.";
            var book = UsfmParser.ParseBook(usfm);
            Assert.IsNotNull(book);
            Assert.AreEqual("TestBook", book.Title);
            Assert.AreEqual(2, book.Chapters.Count);
            Assert.AreEqual(2, book.Chapters[0].Verses.Count);
            Assert.AreEqual(2, book.Chapters[1].Index);
            Assert.IsTrue(book.Chapters[0].ToHtml().Contains("<h2>1</h2>"));
            Assert.IsTrue(book.Chapters[0].ToDisplayLines().Any(l => l.IsVerse));
        }

        [TestMethod]
        public void ParseBook_ShouldHandleInlineItalicsAndFootnotes()
        {
            var usfm = "\\id T\\n\\h T\\n\\c 1\\n\\v 1 This is \\em italic \\em* and a footnote \\f text of footnote \\f*.";
            var book = UsfmParser.ParseBook(usfm);
            Assert.IsNotNull(book);
            Assert.AreEqual(1, book.Chapters.Count);
            var ch = book.Chapters[0];
            Assert.IsTrue(ch.Verses[0].Text.Contains("<em>italic</em>") || ch.Verses[0].Text.Contains("italic"));
            Assert.IsTrue(ch.Footnotes.Count == 1);
        }

        [TestMethod]
        public void ParseBook_ShouldStripExtendedMarkers()
        {
            var usfm = "\\id T\\n\\h T\\n\\c 1\\n\\v 1 \\nd \\+w Lord|strong=\\\"H3068\\\"\\+w*\\nd* reigns.";
            var book = UsfmParser.ParseBook(usfm);
            var text = book.Chapters[0].Verses[0].Text;
            Assert.IsFalse(text.Contains("\\nd"));
            Assert.IsFalse(text.Contains("\\+w"));
            Assert.IsFalse(text.Contains(" d "));
            Assert.IsFalse(text.Contains("d*"));
            Assert.IsTrue(text.Contains("Lord"));
        }

        [TestMethod]
        public void ParseBook_ShouldStripClosingMarkerBeforePunctuation()
        {
            var usfm = "\\id T\\n\\h T\\n\\c 1\\n\\v 1 Rise up, \\nd Lord\\nd*!";
            var book = UsfmParser.ParseBook(usfm);
            var text = book.Chapters[0].Verses[0].Text;
            Assert.IsFalse(text.Contains("\\nd"));
            Assert.IsFalse(text.Contains("\\nd*"));
            Assert.IsTrue(text.Contains("Lord!"));
        }
    }
}
