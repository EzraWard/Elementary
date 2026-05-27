using Elementary.Core.Dictionaries;
using Elementary.Core.Enums;
using Elementary.Core.Interfaces;
using Elementary.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Elementary.Core.Services
{
    public class SearchService : ISearchService
    {
        private readonly IBibleService _bibleService;

        public SearchService(IBibleService bibleService)
        {
            _bibleService = bibleService ?? throw new ArgumentNullException(nameof(bibleService));
        }

        public async Task<List<SearchResult>> SearchAsync(Bible bible, ETranslation translation, string query, ESearchScope scope)
        {
            var results = new List<SearchResult>();
            if (bible?.Books == null || string.IsNullOrWhiteSpace(query))
            {
                return results;
            }

            var normalizedQuery = query.Trim();
            var booksToSearch = GetBooksForScope(bible, scope);

            foreach (var book in booksToSearch)
            {
                await _bibleService.EnsureBookLoaded(translation, book);

                if (book.Chapters == null)
                {
                    continue;
                }

                string bookKey = null;
                if (EBookToLocation.EBookTitleToEBook.TryGetValue(book.Title, out var bookEnum))
                {
                    bookKey = bookEnum.ToString();
                }

                foreach (var chapter in book.Chapters)
                {
                    if (chapter.DisplayLines == null)
                    {
                        continue;
                    }

                    foreach (var line in chapter.DisplayLines)
                    {
                        if (line.Type != ChapterDisplayLineType.Verse || line.VerseNumber <= 0 || string.IsNullOrWhiteSpace(line.Text))
                        {
                            continue;
                        }

                        if (line.Text.IndexOf(normalizedQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            results.Add(new SearchResult
                            {
                                BookTitle = book.Title,
                                BookKey = bookKey,
                                ChapterIndex = chapter.Index,
                                VerseNumber = line.VerseNumber,
                                VerseText = line.Text
                            });
                        }
                    }
                }
            }

            return results;
        }

        private static List<Book> GetBooksForScope(Bible bible, ESearchScope scope)
        {
            if (scope == ESearchScope.EntireBible)
            {
                return bible.Books.ToList();
            }

            var filtered = new List<Book>();
            foreach (var book in bible.Books)
            {
                if (!EBookToLocation.EBookTitleToEBook.TryGetValue(book.Title, out var bookEnum))
                {
                    continue;
                }

                var enumValue = (int)bookEnum;

                if (scope == ESearchScope.OldTestament && enumValue >= (int)EBook.Genesis && enumValue <= (int)EBook.Malachi)
                {
                    filtered.Add(book);
                }
                else if (scope == ESearchScope.NewTestament && enumValue >= (int)EBook.Matthew && enumValue <= (int)EBook.Revelation)
                {
                    filtered.Add(book);
                }
            }

            return filtered;
        }
    }
}
