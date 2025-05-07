using Elementary.Core.Dictionaries;
using Elementary.Core.Enums;
using Elementary.Core.Interfaces;
using Elementary.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using VersOne.Epub;

namespace Elementary.Core.Services
{
    public class BibleService : IBibleService
    {
        private readonly ISettingsService _settingsService;
        private readonly IFileService _fileService;
        private readonly IFilePathProvider _filePathProvider;
        private readonly ISettings _settings;

        public BibleService(ISettingsService settingsService, IFileService fileService, IFilePathProvider filePathProvider)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(settingsService)); ;
            _filePathProvider = filePathProvider ?? throw new ArgumentNullException(nameof(settingsService));

            _settings = _settingsService.GetSettings();
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
            EpubBook epubBible;

            var bibleFilePath = _filePathProvider.GetPathForTranslation(ETranslation.NET);

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
                    numberOfChapters = 22;
                }

                //Set
                bible.Books[i].Chapters = new ObservableCollection<Chapter>();
                for (int j = 1; j < numberOfChapters; j++)
                {
                    var readingOrderIndex = bible.Books[i].ReadingOrderIndex + j;
                    bible.Books[i].Chapters.Add(new Chapter { Index = j, ChapterText = epubBible.ReadingOrder[readingOrderIndex].Content });
                }
            }

            return bible;
        }
    }
}