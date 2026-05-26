using Elementary.Core.Dictionaries;
using Elementary.Core.Enums;
using Elementary.Core.Extensions;
using Elementary.Core.Interfaces;
using Elementary.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Elementary.Core.Services
{
    public class BibleService : IBibleService
    {
        private readonly ISettingsService _settingsService;
        private readonly IFileService _fileService;
        private readonly IFilePathProvider _filePathProvider;
        private readonly Dictionary<ETranslation, Bible> _bibleCache = new Dictionary<ETranslation, Bible>();

        public BibleService(ISettingsService settingsService, IFileService fileService, IFilePathProvider filePathProvider)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _filePathProvider = filePathProvider ?? throw new ArgumentNullException(nameof(filePathProvider));
        }

        public async Task<Bible> GetBible(ETranslation translation)
        {
            if (_bibleCache.TryGetValue(translation, out var cachedBible))
            {
                return cachedBible;
            }

            Bible bible;
            switch (translation)
            {
                case ETranslation.ASV:
                    bible = await GetBibleFromTranslation(translation);
                    break;
                case ETranslation.KJV:
                    bible = await GetBibleFromTranslation(translation);
                    break;
                case ETranslation.NET:
                    bible = await GetBibleFromTranslation(translation);
                    break;
                default:
                    return null;
            }

            _bibleCache[translation] = bible;
            return bible;
        }

        public async Task EnsureBookLoaded(ETranslation translation, Book book)
        {
            if (book == null || book.IsChaptersLoaded) return;

            if (string.IsNullOrWhiteSpace(book.SourcePath))
            {
                if (book.Chapters == null) book.Chapters = new ObservableCollection<Chapter>();
                book.IsChaptersLoaded = true;
                return;
            }

            var chapters = new ObservableCollection<Chapter>();

            try
            {
                using (var stream = await _fileService.ReadFileAsync(book.SourcePath))
                using (var reader = new StreamReader(stream))
                {
                    var content = await reader.ReadToEndAsync();
                    var usfmBook = Elementary.Core.Parsers.UsfmParser.ParseBook(content);

                    if (usfmBook != null)
                    {
                        if (!string.IsNullOrWhiteSpace(usfmBook.Title))
                        {
                            book.Title = usfmBook.Title;
                        }

                        foreach (var ch in usfmBook.Chapters)
                        {
                            chapters.Add(new Chapter
                            {
                                Index = ch.Index,
                                ChapterText = ch.ToHtml(),
                                DisplayLines = new ObservableCollection<ChapterDisplayLine>(ch.ToDisplayLines())
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load chapters for {book.SourcePath}: {ex}");
            }

            book.Chapters = chapters;
            book.ChapterCount = chapters.Count;
            book.IsChaptersLoaded = true;
        }

        private async Task<Bible> GetBibleFromTranslation(ETranslation translation)
        {
            var bible = new Bible();
            var bibleFilePath = _filePathProvider.GetPathForTranslation(translation);

            // Assume a folder path containing USFM files
            var usfmFiles = await _fileService.ListFilesAsync(bibleFilePath, "*.usfm");
            var fileList = new List<string>(usfmFiles ?? new string[0]);

            // Sort files by name to get canonical order if filenames are numbered
            fileList.Sort();

            foreach (var filePath in fileList)
            {
                try
                {
                    var book = await CreateBookAsync(filePath);
                    bible.Books.Add(book);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to read USFM title from {filePath}: {ex}");
                }
            }

            return bible;
        }

        private async Task<Book> CreateBookAsync(string filePath)
        {
            var metadata = GetCanonicalMetadata(filePath);
            if (metadata != null)
            {
                return new Book
                {
                    Title = metadata.Title,
                    ReadingOrderIndex = metadata.ReadingOrderIndex,
                    ChapterCount = metadata.ChapterCount,
                    SourcePath = filePath,
                    Chapters = new ObservableCollection<Chapter>(),
                    IsChaptersLoaded = false
                };
            }

            var bookTitle = await ReadUsfmBookTitleAsync(filePath);
            return new Book
            {
                Title = bookTitle,
                SourcePath = filePath,
                Chapters = new ObservableCollection<Chapter>(),
                IsChaptersLoaded = false
            };
        }

        private static CanonicalBookMetadata GetCanonicalMetadata(string filePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            var codeMatch = Regex.Match(fileName, @"^\d{2}-(?<code>[1-3]?[A-Z]{3})", RegexOptions.IgnoreCase);
            if (!codeMatch.Success)
            {
                return null;
            }

            var usfmCode = codeMatch.Groups["code"].Value;
            if (!EBookToLocation.UsfmCodeToEBook.TryGetValue(usfmCode, out var canonicalBook)
                || !EBookToLocation.EBookToEPubLocationNET.TryGetValue(canonicalBook, out var readingOrderIndex)
                || !EBookToLocation.EBookToChapterCount.TryGetValue(canonicalBook, out var chapterCount))
            {
                return null;
            }

            return new CanonicalBookMetadata
            {
                Title = canonicalBook.GetDisplayName(),
                ReadingOrderIndex = readingOrderIndex,
                ChapterCount = chapterCount
            };
        }

        private sealed class CanonicalBookMetadata
        {
            public string Title { get; set; }
            public int ReadingOrderIndex { get; set; }
            public int ChapterCount { get; set; }
        }

        private async Task<string> ReadUsfmBookTitleAsync(string filePath)
        {
            try
            {
                using (var stream = await _fileService.ReadFileAsync(filePath))
                using (var reader = new StreamReader(stream))
                {
                    string line;
                    string idFallback = null;
                    int lineCount = 0;

                    while ((line = await reader.ReadLineAsync()) != null && lineCount < 300)
                    {
                        lineCount++;
                        var trimmed = line.Trim();
                        if (string.IsNullOrWhiteSpace(trimmed)) continue;

                        var hMatch = Regex.Match(trimmed, @"^\\h\s+(.+)$", RegexOptions.IgnoreCase);
                        if (hMatch.Success) return hMatch.Groups[1].Value.Trim();

                        var mtMatch = Regex.Match(trimmed, @"^\\mt\d*\s+(.+)$", RegexOptions.IgnoreCase);
                        if (mtMatch.Success) return mtMatch.Groups[1].Value.Trim();

                        var idMatch = Regex.Match(trimmed, @"^\\id\s+(.+)$", RegexOptions.IgnoreCase);
                        if (idMatch.Success)
                        {
                            idFallback = idMatch.Groups[1].Value.Trim();
                        }

                        if (trimmed.StartsWith("\\c ", StringComparison.OrdinalIgnoreCase))
                        {
                            break;
                        }
                    }

                    return !string.IsNullOrWhiteSpace(idFallback) ? idFallback : Path.GetFileNameWithoutExtension(filePath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Falling back to filename for USFM title: {filePath}. Error: {ex}");
                return Path.GetFileNameWithoutExtension(filePath);
            }
        }

        public static string CleanVerseMarkers(string html)
        {
            // Pattern to match verse markers like <span class="verse">1:1</span>, <span class="verse">2:15</span>, etc.
            // This captures the chapter number, colon, and verse number
            string pattern = @"<span class=""verse"">(\d+):(\d+)</span>";

            // Replace with superscript version containing only the verse number
            string replacement = "<sup>$2</sup>";

            return Regex.Replace(html, pattern, replacement);
        }

        public static string RemoveVerseMarkers(string html)
        {
            // Pattern to match verse markers like <span class="verse">1:1</span>, <span class="verse">2:15</span>, etc.
            // This captures the chapter number, colon, and verse number
            string pattern = @"<span class=""verse"">(\d+):(\d+)</span>";

            // Completeley remove the verse markers
            string replacement = "";

            return Regex.Replace(html, pattern, replacement);
        }
    }
}
