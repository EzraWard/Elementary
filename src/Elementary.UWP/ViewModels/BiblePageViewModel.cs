using CommunityToolkit.Mvvm.ComponentModel;
using Elementary.Core.Enums;
using Elementary.Core.Interfaces;
using Elementary.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Elementary.ViewModels
{
    public partial class BiblePageViewModel : ObservableObject
    {
        private Bible _bible;
        private Book _currentBook;
        private Chapter _currentChapter;
        private int _selectedChapterIndex;
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
                if (SetProperty(ref _currentBook, value))
                {
                    OnPropertyChanged(nameof(ChapterIndices));

                    // Automatically update chapter when book changes
                    CurrentChapter = _currentBook?.Chapters.FirstOrDefault();
                    //SelectedChapterIndex = CurrentChapter?.Index ?? 1;
                }
            }
        }

        public Chapter CurrentChapter
        {
            get => _currentChapter;
            set => SetProperty(ref _currentChapter, value);
        }

        public List<int> ChapterIndices =>
            CurrentBook?.Chapters != null
                ? Enumerable.Range(1, _currentBook.Chapters.Count).ToList()
                : new List<int>();

        public int SelectedChapterIndex
        {
            get => _selectedChapterIndex;
            set 
            {
                if (SetProperty(ref _selectedChapterIndex, value))
                {
                    // Update CurrentChapter when SelectedChapterIndex changes
                    if (CurrentBook?.Chapters != null)
                    {
                        // Find chapter by index (assuming 1-based indexing)
                        CurrentChapter = CurrentBook.Chapters.FirstOrDefault(c => c.Index == value) 
                                      ?? CurrentBook.Chapters.ElementAtOrDefault(value - 1);
                    }
                }
            }
        }

        public ISettings AppSettings
        {
            get => _appSettings;
            set => SetProperty(ref _appSettings, value);
        }

        public BiblePageViewModel()
        {}

        public async Task Initialize()
        {
            var settingsService = App.Services.GetRequiredService<ISettingsService>();
            AppSettings = settingsService.GetSettings();

            var _bibleService = App.Services.GetRequiredService<IBibleService>();
            Bible = await _bibleService.GetBible(ETranslation.NET);

            CurrentBook = Bible.Books.FirstOrDefault(b => b.Title == AppSettings.Book.ToString()) ?? Bible.Books.FirstOrDefault();
            CurrentChapter = CurrentBook?.Chapters.FirstOrDefault(c => c.Index == AppSettings.Chapter) ?? CurrentBook?.Chapters.FirstOrDefault() ?? new Chapter();
            SelectedChapterIndex = CurrentChapter.Index;
        }
    }
}