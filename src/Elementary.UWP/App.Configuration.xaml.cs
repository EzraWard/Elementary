using Elementary.Core.Interfaces;
using Elementary.Core.Services;
using Elementary.Services;
using Elementary.VerseOfTheDay.Interfaces;
using Elementary.VerseOfTheDay.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;

namespace Elementary
{
    partial class App
    {
        private IServiceProvider _serviceProvider;

        public static IServiceProvider Services
        {
            get
            {
                IServiceProvider serviceProvider = ((App)Current)._serviceProvider
                    ?? throw new InvalidOperationException("The service provider is not initialized.");
                return serviceProvider;
            }
        }

        private static IServiceProvider ConfigureServices()
        {
            var provider = new ServiceCollection();

            // Register the file path provider
            provider.AddSingleton<IFilePathProvider, WindowsFilePathProvider>();

            // Register the file service
            provider.AddSingleton<IFileService, UWPFileService>();

            // Register the settings provider
            provider.AddSingleton<ISettingsProvider, WindowsSettingsProvider>();

            // Register the settings service
            provider.AddSingleton<ISettingsService, SettingsService>();

            // Register the Bible service
            provider.AddSingleton<IBibleService, BibleService>();

            // Register the search service
            provider.AddSingleton<ISearchService, SearchService>();

            // Register shared HttpClient for VOTD services
            provider.AddSingleton<HttpClient>(_ =>
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "Elementary-Bible-App/1.0");
                client.Timeout = TimeSpan.FromSeconds(30);
                return client;
            });

            // Register Verse of the Day services
            provider.AddSingleton<IVotdCacheService, VotdCacheService>();
            provider.AddSingleton<IVotdStorageService, UwpVotdStorageService>();
            provider.AddSingleton<IVerseFetchService, VerseFetchService>();
            provider.AddSingleton<IUnsplashService, UnsplashService>();
            provider.AddSingleton<IVotdImageCompositor, VotdImageCompositor>();
            provider.AddSingleton<IVerseOfTheDayService, Elementary.VerseOfTheDay.Services.VerseOfTheDayService>();

            // Register tile update service
            provider.AddSingleton<ITileUpdateService, TileUpdateService>();

            // Register the VOTD dialog service (in-app ContentDialog display)
            provider.AddSingleton<IVerseOfTheDayDialogService, VerseOfTheDayDialogService>();

            return provider.BuildServiceProvider(true);
        }
    }
}
