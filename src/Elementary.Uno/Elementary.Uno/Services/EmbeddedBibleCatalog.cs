using Elementary.Core.Dictionaries;
using Elementary.Core.Enums;
using Elementary.Core.Models;
using Elementary.Core.Parsers;
using System.IO.Compression;
using System.Reflection;

namespace Elementary.Uno.Services;

public sealed class ScriptureBook(EBook id, string key, string title, string resourceName)
{
    public EBook Id { get; } = id;
    public string Key { get; } = key;
    public string Title { get; } = title;
    public string ResourceName { get; } = resourceName;
}

public sealed class EmbeddedBibleCatalog
{
    private readonly Assembly _assembly;
    private readonly Dictionary<string, UsfmBook> _cache = new();
    private readonly Dictionary<UsfmChapter, List<ChapterDisplayLine>> _lines = new();
    public IReadOnlyList<ScriptureBook> Books { get; private set; } = Array.Empty<ScriptureBook>();
    public ETranslation Translation { get; private set; }

    public EmbeddedBibleCatalog(Assembly assembly, ETranslation translation = ETranslation.KJV)
    {
        _assembly = assembly;
        SetTranslation(translation);
    }

    public void SetTranslation(ETranslation translation)
    {
        var resources = _assembly.GetManifestResourceNames();
        Books = EBookToLocation.UsfmCodeToEBook.OrderBy(pair => (int)pair.Value)
            .Select(pair => new ScriptureBook(pair.Value, pair.Key,
                EBookToLocation.EBookTitleToEBook.First(title => title.Value == pair.Value).Key,
                resources.FirstOrDefault(name => name == $"Elementary.Uno.Scripture.{translation}.{pair.Key}.bible")
                // Source-resource fallback retains compatibility with shared parser tests.
                ?? resources.Single(name => name.StartsWith("Elementary.Uno.Scripture.", StringComparison.Ordinal)
                    && name.Contains("-" + pair.Key + "eng-", StringComparison.Ordinal))))
            .ToArray();
        Translation = translation;
    }

    public IReadOnlyList<ChapterDisplayLine> DisplayLines(UsfmChapter chapter)
    {
        if (!_lines.TryGetValue(chapter, out var lines)) _lines[chapter] = lines = chapter.ToDisplayLines();
        return lines;
    }

    public async Task<UsfmBook> LoadAsync(ScriptureBook book)
    {
        if (_cache.TryGetValue(book.ResourceName, out var cached)) return cached;
        await using var stream = _assembly.GetManifestResourceStream(book.ResourceName)
            ?? throw new InvalidOperationException($"Missing scripture: {book.Title}");
        UsfmBook parsed;
        if (book.ResourceName.EndsWith(".bible", StringComparison.Ordinal))
        {
            using var compressed = new GZipStream(stream, CompressionMode.Decompress);
            using var reader = new BinaryReader(compressed);
            if (reader.ReadInt32() != 1) throw new InvalidDataException("Unsupported scripture format.");
            parsed = new UsfmBook { Title = reader.ReadString() };
            var chapters = reader.ReadInt32();
            for (var i = 0; i < chapters; i++)
            {
                var chapter = new UsfmChapter { Index = reader.ReadInt32() };
                var verses = reader.ReadInt32();
                for (var j = 0; j < verses; j++) chapter.Verses.Add(new UsfmVerse { Number = reader.ReadInt32(), Text = reader.ReadString() });
                var notes = reader.ReadInt32();
                for (var j = 0; j < notes; j++) chapter.Footnotes.Add(reader.ReadString());
                var count = reader.ReadInt32();
                var lines = new List<ChapterDisplayLine>(count);
                for (var j = 0; j < count; j++) lines.Add(new ChapterDisplayLine { Type = (ChapterDisplayLineType)reader.ReadInt32(), VerseNumber = reader.ReadInt32(), Text = reader.ReadString() });
                _lines[chapter] = lines;
                parsed.Chapters.Add(chapter);
                if (i % 20 == 19) await Task.Delay(1);
            }
        }
        else
        {
            using var reader = new StreamReader(stream);
            parsed = UsfmParser.ParseBook(await reader.ReadToEndAsync());
        }
        _cache[book.ResourceName] = parsed;
        return parsed;
    }
}
