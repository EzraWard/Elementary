using Elementary.VerseOfTheDay.Services;
using Elementary.WidgetApp.ComInfrastructure;
using Elementary.WidgetApp.Services;
using System.Linq;
using System.Net.Http;
using System;

namespace Elementary.WidgetApp
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            WinRT.ComWrappersSupport.InitializeComWrappers();

            // Only run as a COM server when launched by the widget host
            if (args?.Any(x => x.Contains("RegisterProcessAsComServer")) ?? false)
            {
                using var lifetime = new WidgetServerLifetime(TimeSpan.FromSeconds(10));
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Elementary-WidgetApp/1.0");

                var cache = new VotdCacheService();
                var storage = new WinAppSdkVotdStorageService();
                var verseFetch = new VerseFetchService(httpClient);
                var compositor = new VotdImageCompositor();
                var votdService = new VerseOfTheDayService(verseFetch, compositor, cache);

                var provider = new WidgetProvider(votdService, storage, lifetime);
                var clsid = typeof(WidgetProvider).GUID;
                var factory = new WidgetProviderClassFactory(provider);

                ComServer.Run(clsid, factory, lifetime.ShutdownSignal);
            }
        }
    }
}
