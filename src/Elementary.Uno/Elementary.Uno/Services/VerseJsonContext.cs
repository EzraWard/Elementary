using Elementary.VerseOfTheDay.Models;
using System.Text.Json.Serialization;

namespace Elementary.Uno.Services;

[JsonSerializable(typeof(BibleVerseData))]
internal partial class VerseJsonContext : JsonSerializerContext { }
