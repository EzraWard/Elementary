using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Elementary.Services
{
    public class LiveTileService : ILiveTileService
    {
        internal const string BackgroundTaskName = "VotDTileUpdateTask";
        private const int DailyUpdateIntervalMinutes = 1440;
        private readonly IVerseOfTheDayService _verseOfTheDayService;

        public LiveTileService(IVerseOfTheDayService verseOfTheDayService)
        {
            _verseOfTheDayService = verseOfTheDayService ?? throw new ArgumentNullException(nameof(verseOfTheDayService));
        }

        public void UpdateTile()
        {
            var verse = _verseOfTheDayService.GetVerseOfTheDay();
            var tileXml = BuildTileXml(verse.ImageUrl);
            var tileDoc = new XmlDocument();
            tileDoc.LoadXml(tileXml);
            var notification = new TileNotification(tileDoc);
            TileUpdateManager.CreateTileUpdaterForApplication().Update(notification);
        }

        public async Task RegisterBackgroundTaskAsync()
        {
            var status = await BackgroundExecutionManager.RequestAccessAsync();
            if (status == BackgroundAccessStatus.DeniedBySystemPolicy ||
                status == BackgroundAccessStatus.DeniedByUser)
            {
                return;
            }

            if (BackgroundTaskRegistration.AllTasks.Any(t => t.Value.Name == BackgroundTaskName))
            {
                return;
            }

            var builder = new BackgroundTaskBuilder
            {
                Name = BackgroundTaskName
            };
            builder.SetTrigger(new TimeTrigger(DailyUpdateIntervalMinutes, false));
            builder.Register();
        }

        private static string BuildTileXml(string imageUrl)
        {
            return $@"<tile>
  <visual>
    <binding template=""TileMedium"">
      <image src=""{imageUrl}"" placement=""background"" hint-overlay=""0""/>
    </binding>
    <binding template=""TileWide"">
      <image src=""{imageUrl}"" placement=""background"" hint-overlay=""0""/>
    </binding>
    <binding template=""TileLarge"">
      <image src=""{imageUrl}"" placement=""background"" hint-overlay=""0""/>
    </binding>
  </visual>
</tile>";
        }
    }
}
