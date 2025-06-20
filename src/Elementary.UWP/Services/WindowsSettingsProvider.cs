using Elementary.Core.Interfaces;
using Windows.Storage;

namespace Elementary.Services
{
    public class WindowsSettingsProvider : ISettingsProvider
    {
        public string GetSetting(string key)
        {
            var setting = ApplicationData.Current.LocalSettings.Values[key];
            if (setting == null) return null;
            return setting.ToString();
        }

        public void SaveSetting(string key, string value)
        {
            ApplicationData.Current.LocalSettings.Values[key] = value;
        }

        public void DeleteSetting(string key)
        {
            var setting = ApplicationData.Current.LocalSettings.Values[key];
            if (setting == null) return;
            ApplicationData.Current.LocalSettings.Values.Remove(key);
        }
    }
}
