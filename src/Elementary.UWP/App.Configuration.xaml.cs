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
            provider.AddSingleton<IFileService, FileService>();

            return provider.BuildServiceProvider(true);
        }
    }
}
