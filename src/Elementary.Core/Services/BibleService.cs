using Elementary.Core.Dictionaries;
using Elementary.Core.Enums;
using Elementary.Core.Interfaces;
using Elementary.Core.Models;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VersOne.Epub;

namespace Elementary.Core.Services
{
    public class BibleService : IBibleService
    {
        private const int RevelationChapterCount = 22;

        private readonly ISettingsService _settingsService;
        private readonly IFileService _fileService;
        private readonly IFilePathProvider _filePathProvider;

        public BibleService(ISettingsService settingsService, IFileService fileService, IFilePathProvider filePathProvider)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _filePathProvider = filePathProvider ?? throw new ArgumentNullException(nameof(filePathProvider));
        }

        public async Task<Bible> GetBible(ETranslation translation)
        {
            switch (translation)
            {
                case ETranslation.ASV:
                    return await GetBibleFromTranslation(translation);
                case ETranslation.KJV:
                    return await GetBibleFromTranslation(translation);
                case ETranslation.NET:
                    return await GetBibleFromTranslation(translation);
                default:
                    return null;
            }
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
            book.IsChaptersLoaded = true;
        }

        private async Task<Bible> GetBibleFromTranslation(ETranslation translation)
        {
            var bible = new Bible();
            var bibleFilePath = _filePathProvider.GetPathForTranslation(translation);

            // If path ends with .epub keep existing behavior
            if (!string.IsNullOrEmpty(bibleFilePath) && bibleFilePath.EndsWith(".epub", StringComparison.OrdinalIgnoreCase))
            {
                EpubBook epubBible;
                using (var stream = await _fileService.ReadFileAsync(bibleFilePath))
                {
                    epubBible = EpubReader.ReadBook(stream);
                }

                //Enumerate Books
                foreach (var book in epubBible.Navigation)
                {
                    bible.Books.Add(new Book
                    {
                        Title = book.Title
                    });
                }

                //Set reading order index for each book
                foreach (var book in bible.Books)
                {
                    var bookEnum = EBookToLocation.EBookTitleToEBook[book.Title];
                    book.ReadingOrderIndex = EBookToLocation.EBookToEPubLocationNET[bookEnum];
                }

                //intialize chapters
                for (int i = 0; i < bible.Books.Count; i++)
                {
                    int numberOfChapters;
                    if (bible.Books[i].Title != "Revelation")
                    {
                        numberOfChapters = bible.Books[i + 1].ReadingOrderIndex - bible.Books[i].ReadingOrderIndex;
                    }
                    else
                    {
                        numberOfChapters = RevelationChapterCount + 1;
                    }

                    //Set
                    bible.Books[i].Chapters = new ObservableCollection<Chapter>();
                    for (int j = 1; j < numberOfChapters; j++)
                    {
                        var readingOrderIndex = bible.Books[i].ReadingOrderIndex + j;
                        var text = epubBible.ReadingOrder[readingOrderIndex].Content;
                        bible.Books[i].Chapters.Add(new Chapter { Index = j, ChapterText = CleanChapterHtml(epubBible.ReadingOrder[readingOrderIndex].Content, bible.Books[i].Title, j) });
                    }
                }

                return bible;
            }

            // Otherwise, assume a folder path containing USFM files
            var usfmFiles = await _fileService.ListFilesAsync(bibleFilePath, "*.usfm");
            var fileList = new List<string>(usfmFiles ?? new string[0]);

            // Sort files by name to get canonical order if filenames are numbered
            fileList.Sort();

            foreach (var filePath in fileList)
            {
                try
                {
                    var bookTitle = await ReadUsfmBookTitleAsync(filePath);
                    bible.Books.Add(new Book
                    {
                        Title = bookTitle,
                        SourcePath = filePath,
                        Chapters = new ObservableCollection<Chapter>(),
                        IsChaptersLoaded = false
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to read USFM title from {filePath}: {ex}");
                }
            }

            // Set reading order index using the mapping when possible
            foreach (var book in bible.Books)
            {
                if (EBookToLocation.EBookTitleToEBook.TryGetValue(book.Title, out var bookEnum))
                {
                    book.ReadingOrderIndex = EBookToLocation.EBookToEPubLocationNET[bookEnum];
                }
            }

            return bible;
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

        private string CleanChapterHtml(string chapterText, string BookName, int chapterNum)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.OptionWriteEmptyNodes = true;
            htmlDoc.LoadHtml(chapterText);

            var html = htmlDoc.DocumentNode.SelectSingleNode("//body").InnerHtml;
            
            //remove book name at the beginning of the books
            var booklessString = html.Replace(BookName + "<br />", "");

            // Always fetch the latest settings
            var settings = _settingsService.GetSettings();
            return settings.ShowVerseNumbers == true ? 
                CleanVerseMarkers(booklessString) : 
                RemoveVerseMarkers(booklessString);
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
