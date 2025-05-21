using CommunityToolkit.Mvvm.ComponentModel;
using Elementary.Core.Enums;
using Elementary.Core.Interfaces;
using Elementary.Core.Models;
using Microsoft.Extensions.DependencyInjection;

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
        }
    }
}