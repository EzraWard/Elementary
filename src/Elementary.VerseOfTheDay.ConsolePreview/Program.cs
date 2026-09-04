using Elementary.VerseOfTheDay.Models;
using Elementary.VerseOfTheDay.Services;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Elementary.VerseOfTheDay.ConsolePreview
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Elementary – Verse of the Day Preview Generator");
            Console.WriteLine("================================================");

            var outputDir = Path.Combine(AppContext.BaseDirectory, "preview-output");
            Directory.CreateDirectory(outputDir);

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Elementary-ConsolePreview/1.0");

            var cache = new VotdCacheService();
            var verseFetch = new VerseFetchService(httpClient);
            var compositor = new VotdImageCompositor();
            var votdService = new VerseOfTheDayService(verseFetch, compositor, cache);

            // Generate a result at a large size first to warm up the cache
            Console.WriteLine("Fetching verse and generating abstract artwork...");

            foreach (VotdImageSize size in Enum.GetValues(typeof(VotdImageSize)))
            {
                Console.Write($"  Compositing {size}... ");
                try
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    var result = await votdService.GetAsync(size);
                    sw.Stop();

                    var filename = $"{size}.png";
                    var path = Path.Combine(outputDir, filename);
                    await File.WriteAllBytesAsync(path, result.ImageBytes);

                    Console.WriteLine($"done ({sw.ElapsedMilliseconds}ms) → {path}");

                    if (size == VotdImageSize.InApp)
                    {
                        Console.WriteLine();
                        Console.WriteLine($"  Verse: {result.VerseText}");
                        Console.WriteLine($"  Reference: {result.Reference}");
                        Console.WriteLine();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"FAILED: {ex.Message}");
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Output written to: {outputDir}");
        }
    }
}
