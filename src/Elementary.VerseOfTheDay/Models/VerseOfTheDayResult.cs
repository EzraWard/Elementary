using System;

namespace Elementary.VerseOfTheDay.Models
{
    public class VerseOfTheDayResult
    {
        public string VerseText { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public byte[] ImageBytes { get; set; } = Array.Empty<byte>();
        public DateTime RetrievedAt { get; set; }
    }
}
