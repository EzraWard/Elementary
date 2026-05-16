using Elementary.VerseOfTheDay.Interfaces;
using Elementary.VerseOfTheDay.Models;
using System;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Elementary.Services
{
    public class TileUpdateService : ITileUpdateService
    {
        private readonly IVotdStorageService _storage;

        public TileUpdateService(IVotdStorageService storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public async Task UpdateAsync(VerseOfTheDayResult result)
        {
            try
            {
                var updater = TileUpdateManager.CreateTileUpdaterForApplication();
                updater.EnableNotificationQueue(true);
                updater.Clear();

                string dateKey = DateTime.UtcNow.ToString("yyyy-MM-dd");
                string largeTileFile = $"{dateKey}_large.png";

                // Persist the large tile image to LocalFolder for the ms-appdata:// URI
                if (result.ImageBytes != null && result.ImageBytes.Length > 0)
                    await _storage.SaveAsync(largeTileFile, result.ImageBytes);

                var tileImageUri = UwpVotdStorageService.GetMsAppDataUri(largeTileFile);

                updater.Update(BuildMediumTile(result.Reference));
                updater.Update(BuildWideTile(result.VerseText, result.Reference));
                updater.Update(BuildLargeTextTile(result.VerseText, result.Reference));
                updater.Update(BuildLargeImageTile(tileImageUri));
            }
            catch (Exception)
            {
                // Tile updates are non-critical; swallow errors silently.
            }
        }

        // Medium (150×150): reference only
        private static TileNotification BuildMediumTile(string reference)
        {
            var xml = $@"<tile>
  <visual>
    <binding template=""TileSquare150x150Text04"">
      <text id=""1"">{EscapeXml(reference)}</text>
    </binding>
  </visual>
</tile>";
            return CreateNotification(xml);
        }

        // Wide (310×150): verse text + reference
        private static TileNotification BuildWideTile(string verseText, string reference)
        {
            var truncated = Truncate(verseText, 160);
            var xml = $@"<tile>
  <visual>
    <binding template=""TileWide310x150Text09"">
      <text id=""1"">{EscapeXml(truncated)}</text>
      <text id=""2"">{EscapeXml(reference)}</text>
    </binding>
  </visual>
</tile>";
            return CreateNotification(xml);
        }

        // Large (310×310): text version
        private static TileNotification BuildLargeTextTile(string verseText, string reference)
        {
            var truncated = Truncate(verseText, 300);
            var xml = $@"<tile>
  <visual>
    <binding template=""TileSquare310x310TextList02"">
      <text id=""1"">{EscapeXml(truncated)}</text>
      <text id=""2"">{EscapeXml(reference)}</text>
    </binding>
  </visual>
</tile>";
            return CreateNotification(xml);
        }

        // Large (310×310): image version
        private static TileNotification BuildLargeImageTile(string imageUri)
        {
            var xml = $@"<tile>
  <visual>
    <binding template=""TileSquare310x310Image"">
      <image id=""1"" src=""{EscapeXml(imageUri)}"" alt=""Verse of the Day""/>
    </binding>
  </visual>
</tile>";
            return CreateNotification(xml);
        }

        private static TileNotification CreateNotification(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            return new TileNotification(doc);
        }

        private static string EscapeXml(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
        }

        private static string Truncate(string text, int maxLength)
            => text.Length <= maxLength ? text : text.Substring(0, maxLength).TrimEnd() + "…";
    }
}
