using Elementary.Core.Interfaces;
using Elementary.Core.Services;
using Elementary.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

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

            //Register the Bible service
            provider.AddSingleton<IBibleService, BibleService>();

            //Register the Verse of the Day service
            provider.AddSingleton<IVerseOfTheDayService, VerseOfTheDayService>();
            provider.AddSingleton<IVerseOfTheDayDialogService, VerseOfTheDayDialogService>();
            provider.AddSingleton<ILiveTileService, LiveTileService>();

            return provider.BuildServiceProvider(true);
        }
    }
}
