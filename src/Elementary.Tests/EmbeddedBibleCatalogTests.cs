using Elementary.Core.Dictionaries;
using Elementary.Core.Enums;
using Elementary.Uno.Services;
using System.Reflection;

namespace Elementary.Tests;

[TestClass]
public class EmbeddedBibleCatalogTests
{
    [TestMethod]
    public async Task EveryCanonicalBookAndChapterHasReadableVerses()
    {
        var catalog = new EmbeddedBibleCatalog(Assembly.GetExecutingAssembly());
        Assert.AreEqual(66, catalog.Books.Count);
        CollectionAssert.AreEqual(Enumerable.Range(1, 66).ToArray(), catalog.Books.Select(book => (int)book.Id).ToArray());
        var total = 0;
        foreach (var book in catalog.Books)
        {
            var content = await catalog.LoadAsync(book);
            using var sourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(book.ResourceName)!;
            using var sourceReader = new StreamReader(sourceStream);
            var reference = Elementary.Core.Parsers.UsfmParser.ParseBook(await sourceReader.ReadToEndAsync());
            CollectionAssert.AreEqual(
                reference.Chapters.SelectMany(chapter => chapter.Verses).Select(verse => $"{verse.Number}:{verse.Text}").ToArray(),
                content.Chapters.SelectMany(chapter => chapter.Verses).Select(verse => $"{verse.Number}:{verse.Text}").ToArray(),
                $"Chapter-by-chapter loading must preserve every verse of {book.Title}.");
            var count = EBookToLocation.EBookToChapterCount[book.Id];
            Assert.AreEqual(count, content.Chapters.Count, book.Title);
            CollectionAssert.AreEqual(Enumerable.Range(1, count).ToArray(), content.Chapters.Select(chapter => chapter.Index).ToArray(), book.Title);
            foreach (var chapter in content.Chapters)
                Assert.IsTrue(chapter.ToDisplayLines().Any(line => line.IsVerse && !string.IsNullOrWhiteSpace(line.Text)), $"{book.Title} {chapter.Index}");
            Assert.AreSame(content, await catalog.LoadAsync(book));
            total += count;
        }
        Assert.AreEqual(1189, total);
        Assert.AreEqual(EBook.Revelation, catalog.Books.Last().Id);
    }
}
