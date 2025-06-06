using CommunityToolkit.Mvvm.ComponentModel;
using Elementary.Core.Enums;
using Elementary.Core.Interfaces;
using Elementary.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;

namespace Elementary.ViewModels
{
    public partial class BiblePageViewModel : ObservableObject
    {
        private Bible _bible;
        private Book _currentBook;
        private Chapter _currentChapter;
        private ISettings _appSettings;

        public Bible Bible
        {
            get => _bible;
            set => SetProperty(ref _bible, value);
        }

        public Book CurrentBook
        {
            get => _currentBook;
            set 
            {
                if(SetProperty(ref _currentBook, value))
                {
                    OnPropertyChanged(nameof(ChapterIndices));
                }
            }
        }
        public Chapter CurrentChapter
        {
            get => _currentChapter;
            set => SetProperty(ref _currentChapter, value);
        }

        public List<int> ChapterIndices =>
            _currentBook?.Chapters != null
                ? Enumerable.Range(1, _currentBook.Chapters.Count).ToList()
                : new List<int>();

        public int SelectedChapterIndex { get; set; }

        public ISettings AppSettings
        {
            get => _appSettings;
            set => SetProperty(ref _appSettings, value);
        }

        public BiblePageViewModel()
        {}

        public async void Initialize()
        {
            var settingsService = App.Services.GetRequiredService<ISettingsService>();
            AppSettings = settingsService.GetSettings();

            var _bibleService = App.Services.GetRequiredService<IBibleService>();
            _bible = await _bibleService.GetBible(ETranslation.NET);

            CurrentBook = _bible.Books.FirstOrDefault(b => b.Title == AppSettings.Book.ToString()) ?? _bible.Books.FirstOrDefault();
            CurrentChapter = CurrentBook?.Chapters.FirstOrDefault(c => c.Index == AppSettings.Chapter) ?? CurrentBook?.Chapters.FirstOrDefault() ?? new Chapter();
            SelectedChapterIndex = CurrentChapter.Index;
        }
    }
}