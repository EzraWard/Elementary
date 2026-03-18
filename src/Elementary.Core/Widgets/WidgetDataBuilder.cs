using Elementary.Models;
using System;

namespace Elementary.Widgets
{
    /// <summary>
    /// Builds Adaptive Card data payloads for the Verse of the Day Windows 11 widget.
    /// </summary>
    public static class WidgetDataBuilder
    {
        /// <summary>
        /// Returns the Adaptive Card data JSON for the given <see cref="VerseOfTheDay"/>.
        /// </summary>
        /// <param name="verse">The verse whose data should be serialised.</param>
        /// <returns>A JSON string suitable for use as the widget data payload.</returns>
        public static string BuildAdaptiveCardData(VerseOfTheDay verse)
        {
            if (verse == null) throw new ArgumentNullException(nameof(verse));

            var imageUrl = EscapeJson(verse.ImageUrl ?? string.Empty);
            var title = EscapeJson(verse.Title ?? string.Empty);

            return $"{{\"imageUrl\":\"{imageUrl}\",\"title\":\"{title}\"}}";
        }

        private static string EscapeJson(string value)
            => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
