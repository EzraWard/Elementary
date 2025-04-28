using Elementary.Core.Enums;
using Elementary.Core.Interfaces;
using Elementary.Core.Models;
using System;
using System.Collections.ObjectModel;
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
            _settings = _settingsService.GetSettings();

            _fileService = fileService;

            _filePathProvider = filePathProvider;
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
            foreach (var book in bible.Books)
            {
                book.ReadingOrderIndex = 1;
            }
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

                bible.Books[i].Chapters = new ObservableCollection<Chapter>();
                for (int j = 1; j < numberOfChapters; j++)
                {
                    bible.Books[i].Chapters.Add(new Chapter { Index = j, ReadingOrderIndex = bible.Books[i].ReadingOrderIndex + j });
                }
            }

            return bible;
        }
    }
}