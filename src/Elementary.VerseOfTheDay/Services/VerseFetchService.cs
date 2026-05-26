using Elementary.VerseOfTheDay.Interfaces;
using Elementary.VerseOfTheDay.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Elementary.VerseOfTheDay.Services
{
    public class VerseFetchService : IVerseFetchService
    {
        private const string ApiUrl = "https://labs.bible.org/api/?passage=votd&type=json";

        private readonly HttpClient _httpClient;
        private BibleVerseData? _lastKnownVerse;

        public VerseFetchService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<BibleVerseData> FetchAsync()
        {
            try
            {
                var json = await _httpClient.GetStringAsync(ApiUrl).ConfigureAwait(false);
                var verse = ParseResponse(json);
                _lastKnownVerse = verse;
                return verse;
            }
            catch (Exception)
            {
                // Fallback to last known verse or a hardcoded default
                return _lastKnownVerse ?? GetDefaultVerse();
            }
        }

        private static BibleVerseData ParseResponse(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            JsonElement entry;
            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                entry = root[0];
            else if (root.ValueKind == JsonValueKind.Object)
                entry = root;
            else
                return GetDefaultVerse();

            var text = entry.TryGetProperty("text", out var t) ? t.GetString() ?? string.Empty : string.Empty;
            var book = entry.TryGetProperty("bookname", out var b) ? b.GetString() ?? string.Empty : string.Empty;
            var chapter = entry.TryGetProperty("chapter", out var c) ? c.GetString() ?? string.Empty : string.Empty;
            var verse = entry.TryGetProperty("verse", out var v) ? v.GetString() ?? string.Empty : string.Empty;

            return new BibleVerseData
            {
                VerseText = NormalizeWhitespace(text),
                Book = book,
                Chapter = chapter,
                Verse = verse
            };
        }

        public static string NormalizeWhitespace(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            // Strip HTML tags
            text = Regex.Replace(text, "<[^>]+>", string.Empty);
            // Collapse whitespace
            text = Regex.Replace(text, @"\s+", " ").Trim();
            return text;
        }

        private static BibleVerseData GetDefaultVerse() => new BibleVerseData
        {
            VerseText = "For God so loved the world, that he gave his only begotten Son, that whosoever believeth in him should not perish, but have everlasting life.",
            Book = "John",
            Chapter = "3",
            Verse = "16"
        };
    }
}
