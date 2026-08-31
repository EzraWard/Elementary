using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Windows.ApplicationModel;
using Windows.Storage;

namespace Elementary.Services
{
    internal static class ReadingStreakDiagnostics
    {
        private const string LogFileName = "reading-streak-diagnostics.log";
        private const string PreviousLogFileName = "reading-streak-diagnostics.previous.log";
        private const long MaximumLogFileBytes = 512 * 1024;
        private static readonly object SyncRoot = new object();
        private static readonly string SessionId = Guid.NewGuid().ToString("N").Substring(0, 8);
        private static bool _sessionHeaderWritten;

        internal static string LogPath => Path.Combine(ApplicationData.Current.LocalFolder.Path, LogFileName);

        internal static void Log(string eventName, string details = null)
        {
            var timestamp = DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture);
            var safeEventName = Sanitize(eventName);
            var safeDetails = Sanitize(details);
            var line = $"{timestamp} | session={SessionId} | event={safeEventName}";
            if (!string.IsNullOrWhiteSpace(safeDetails))
            {
                line += $" | {safeDetails}";
            }

            Debug.WriteLine($"[ReadingStreak] {line}");

            try
            {
                lock (SyncRoot)
                {
                    RotateLogIfNeeded();
                    if (!_sessionHeaderWritten)
                    {
                        var version = Package.Current.Id.Version;
                        var versionText = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
                        var header = $"{timestamp} | session={SessionId} | event=session-start | appVersion={versionText}";
                        File.AppendAllText(LogPath, header + Environment.NewLine, Encoding.UTF8);
                        _sessionHeaderWritten = true;
                    }

                    File.AppendAllText(LogPath, line + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ReadingStreak] Unable to persist diagnostics: {ex.Message}");
            }
        }

        private static void RotateLogIfNeeded()
        {
            var logPath = LogPath;
            if (!File.Exists(logPath) || new FileInfo(logPath).Length < MaximumLogFileBytes)
            {
                return;
            }

            var previousLogPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, PreviousLogFileName);
            if (File.Exists(previousLogPath))
            {
                File.Delete(previousLogPath);
            }

            File.Move(logPath, previousLogPath);
        }

        private static string Sanitize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Replace('\r', ' ').Replace('\n', ' ').Replace('|', '/');
        }
    }
}
