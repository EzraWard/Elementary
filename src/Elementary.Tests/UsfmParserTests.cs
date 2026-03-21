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

        [TestMethod]
        public void ParseBook_ShouldParsePoetryMarkersWithIndentation()
        {
            var usfm = "\\id T\\n\\h T\\n\\c 1\\n\\q2 Sing to the Lord";
            var book = UsfmParser.ParseBook(usfm);
            var chapter = book.Chapters[0];

            Assert.AreEqual(1, chapter.Verses.Count);
            Assert.AreEqual(0, chapter.Verses[0].Number);
            Assert.IsTrue(chapter.Verses[0].Text.Contains("class=\"poetry\""));
            Assert.IsTrue(chapter.Verses[0].Text.Contains("margin-left:40px"));
            Assert.IsTrue(chapter.ToDisplayLines().Any(l => l.Type == Elementary.Core.Models.ChapterDisplayLineType.Poetry));
        }

        [TestMethod]
        public void ParseBook_ToDisplayLines_ShouldIncludeHeadingParagraphAndVerse()
        {
            var usfm = "\\id T\\n\\h T\\n\\c 1\\n\\s1 The Heading\\n\\p\\n\\v 1 Verse text";
            var book = UsfmParser.ParseBook(usfm);
            var lines = book.Chapters[0].ToDisplayLines();

            Assert.IsTrue(lines.Any(l => l.Type == Elementary.Core.Models.ChapterDisplayLineType.Heading && l.Text.Contains("The Heading")));
            Assert.IsTrue(lines.Any(l => l.Type == Elementary.Core.Models.ChapterDisplayLineType.ParagraphBreak));
            Assert.IsTrue(lines.Any(l => l.Type == Elementary.Core.Models.ChapterDisplayLineType.Verse && l.VerseNumber == 1));
        }
    }
}
