using Elementary.Core.Enums;
using Elementary.Core.Interfaces;
using Elementary.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Elementary.Core.Services
{
    public class BibleService : IBibleService
    {
        public Bible GetBible(ETranslation translation)
        {
            switch (translation)
            {
                case ETranslation.ASV:
                    return GetBibleASV();
                case ETranslation.KJV:
                    return GetBibleKJV();
                case ETranslation.NET:
                    return GetBibleNET();
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

        private Bible GetBibleNET()
        {
            return new Bible();

        //    var biblePath = BiblePathDictionary.BibleDictionary[AppSettings.Translation.ToString()];
        //    var bibleFilePath = StorageFile.GetFileFromApplicationUriAsync(new Uri(biblePath)).AsTask().Result.Path;

        //    _currentBible = EpubReader.ReadBook(bibleFilePath);

        //    //Enumerate Books
        //    _currentBibleBooks = new List<string>();
        //    Bible = new Bible();
        //    foreach (var book in _currentBible.Navigation)
        //    {
        //        Bible.Books.Add(new Book
        //        {
        //            Title = book.Title
        //        });
        //    }
        //    foreach (var book in Bible.Books)
        //    {
        //        book.ReadingOrderIndex = 1;
        //    }
        //    for (int i = 0; i < Bible.Books.Count; i++)
        //    {
        //        int numberOfChapters;
        //        if (Bible.Books[i].Title != "Revelation")
        //        {
        //            numberOfChapters = Bible.Books[i + 1].ReadingOrderIndex - Bible.Books[i].ReadingOrderIndex;
        //        }
        //        else
        //        {
        //            numberOfChapters = 22;
        //        }

        //        Bible.Books[i].Chapters = new ObservableCollection<Chapter>();
        //        for (int j = 1; j < numberOfChapters; j++)
        //        {
        //            Bible.Books[i].Chapters.Add(new Chapter { Index = j, ReadingOrderIndex = Bible.Books[i].ReadingOrderIndex + j });
        //        }
        //    }

        //    // = Bible.Books.SingleOrDefault(i => i.Title == (string) AppSettings.Book);
        //    //Chapter = Book.Chapters[int.Parse((string) currentChapter) - 1];

        //    //First chapter in Genesis

        //    //var content = _currentBible.ReadingOrder[Chapter.ReadingOrderIndex].Content;
        //    //var match = Regex.Match(content, "(.*<\\s* body[^>]*>)| (<\\s */\\s* body\\s *\\>.+)");
        //    var htmlDoc = new HtmlDocument();
        //    htmlDoc.OptionWriteEmptyNodes = true;
        //    htmlDoc.LoadHtml(_currentBible.ReadingOrder[Chapter.ReadingOrderIndex].Content);
        //    //foreach (var brTag in htmlDoc.DocumentNode.SelectNodes("//br"))
        //    //    brTag.Remove();
        //    CurrentChapterContent = htmlDoc.DocumentNode.SelectSingleNode("//body").InnerHtml;
        //}
        }
    }
}