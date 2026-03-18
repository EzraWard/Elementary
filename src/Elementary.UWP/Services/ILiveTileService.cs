using System.Threading.Tasks;

namespace Elementary.Services
{
    public interface ILiveTileService
    {
        /// <summary>
        /// Updates the application's live tile with the current Verse of the Day image.
        /// </summary>
        void UpdateTile();

        /// <summary>
        /// Registers a daily background task that updates the live tile even when the app is closed.
        /// Requests background execution access if not already granted. Does nothing if the task is
        /// already registered or if the user or system has denied background access.
        /// </summary>
        Task RegisterBackgroundTaskAsync();
    }
}
