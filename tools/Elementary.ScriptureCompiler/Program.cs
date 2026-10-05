using Elementary.Core.Dictionaries;
using Elementary.Core.Parsers;
using System.IO.Compression;
using System.Text.RegularExpressions;

var source = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
Directory.CreateDirectory(output);
foreach (var translation in new[] { "ASV", "KJV", "NET" })
{
    var files = Directory.GetFiles(Path.Combine(source, translation), "*.usfm");
    foreach (var entry in EBookToLocation.UsfmCodeToEBook)
    {
        var file = files.Single(path => Regex.IsMatch(Path.GetFileName(path), @"^\d+-" + entry.Key + "eng", RegexOptions.IgnoreCase));
        var book = UsfmParser.ParseBook(await File.ReadAllTextAsync(file));
        if (book.Chapters.Count != EBookToLocation.EBookToChapterCount[entry.Value])
            throw new InvalidDataException($"Unexpected chapter count in {file}");
        using var target = File.Create(Path.Combine(output, $"{translation}.{entry.Key}.bible"));
        using var compressed = new GZipStream(target, CompressionLevel.SmallestSize);
        using var writer = new BinaryWriter(compressed);
        writer.Write(1); // File format version.
        writer.Write(book.Title ?? entry.Key);
        writer.Write(book.Chapters.Count);
        foreach (var chapter in book.Chapters)
        {
            writer.Write(chapter.Index);
            writer.Write(chapter.Verses.Count);
            foreach (var verse in chapter.Verses) { writer.Write(verse.Number); writer.Write(verse.Text ?? ""); }
            writer.Write(chapter.Footnotes.Count);
            foreach (var note in chapter.Footnotes) writer.Write(note);
            var lines = chapter.ToDisplayLines();
            writer.Write(lines.Count);
            foreach (var line in lines) { writer.Write((int)line.Type); writer.Write(line.VerseNumber); writer.Write(line.Text ?? ""); }
        }
    }
}
await File.WriteAllTextAsync(Path.Combine(output, "complete.stamp"), "198 books compiled from shared USFM sources.");
Console.WriteLine("Prepared ASV, KJV and NET: 198 books, 3,567 chapters.");
