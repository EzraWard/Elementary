using Elementary.Core.Dictionaries;
using Elementary.Core.Enums;
using Elementary.Core.Interfaces;
using Elementary.Core.Models;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VersOne.Epub;

namespace Elementary.Core.Services
{
    public class BibleService : IBibleService
    {
        private readonly ISettingsService _settingsService;
        private readonly IFileService _fileService;
        private readonly IFilePathProvider _filePathProvider;

        public BibleService(ISettingsService settingsService, IFileService fileService, IFilePathProvider filePathProvider)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(settingsService));
            _filePathProvider = filePathProvider ?? throw new ArgumentNullException(nameof(settingsService));
        }

        public async Task<Bible> GetBible(ETranslation translation)
        {
            switch (translation)
            {
                case ETranslation.ASV:
                    return GetBibleASV();
                case ETranslation.KJV:
                    return GetBibleKJV();
                case ETranslation.NET:
                    return await GetBibleNET();
                default:
                    return null;
            }
        }

        private Bible GetBibleASV()
        {
            throw new NotImplementedException();
        }

        private Bible GetBibleKJV()
        {
            throw new NotImplementedException();
        }

        private async Task<Bible> GetBibleNET()
        {
            var bible = new Bible();
            var bibleFilePath = _filePathProvider.GetPathForTranslation(ETranslation.NET);

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
                        numberOfChapters = 23; //this is wrong, but it works for now...
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
                    using (var stream = await _fileService.ReadFileAsync(filePath))
                    using (var reader = new StreamReader(stream))
                    {
                        var content = await reader.ReadToEndAsync();

                        // Extract book title from \h or \mt fields if available
                        var titleMatch = System.Text.RegularExpressions.Regex.Match(content, @"\\h\s+(.+)", System.Text.RegularExpressions.RegexOptions.Multiline);
                        if (!titleMatch.Success)
                        {
                            titleMatch = System.Text.RegularExpressions.Regex.Match(content, @"\\mt\s+(.+)", System.Text.RegularExpressions.RegexOptions.Multiline);
                        }

                        var bookTitle = titleMatch.Success ? titleMatch.Groups[1].Value.Trim() : Path.GetFileNameWithoutExtension(filePath);

                        var book = new Book { Title = bookTitle };
                        book.Chapters = new ObservableCollection<Chapter>();

                        // Split into chapter sections by \c markers
                        var chapterSections = System.Text.RegularExpressions.Regex.Split(content, "(?=\\\\c\\s+\\d+)");

                        foreach (var section in chapterSections)
                        {
                            var chapMatch = System.Text.RegularExpressions.Regex.Match(section, "\\\\c\\s+(\\d+)");
                            if (!chapMatch.Success)
                                continue;

                            var chapNum = int.Parse(chapMatch.Groups[1].Value);

                            // Find all verses in the chapter
                            var verses = System.Text.RegularExpressions.Regex.Matches(section, "\\\\v\\s+(\\d+)\\s+([^\\\\]+|(?:\\\\(?!v|c)[^\\\\]+)*)", System.Text.RegularExpressions.RegexOptions.Singleline);

                            var sb = new System.Text.StringBuilder();
                            foreach (System.Text.RegularExpressions.Match v in verses)
                            {
                                var vnum = v.Groups[1].Value;
                                var vtext = v.Groups[2].Value.Trim().Replace("\n", " ").Replace("\r", " ");

                                // Simple HTML output for a verse
                                sb.Append($"<sup>{vnum}</sup> {System.Net.WebUtility.HtmlEncode(vtext)} ");
                            }

                            book.Chapters.Add(new Chapter { Index = chapNum, ChapterText = sb.ToString() });
                        }

                        bible.Books.Add(book);
                    }
                }
                catch
                {
                    // Ignore problematic files and continue
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