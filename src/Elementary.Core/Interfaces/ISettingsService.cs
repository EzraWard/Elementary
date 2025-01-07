namespace Elementary.Core.Interfaces
{
    public interface ISettingsService
    {
        ISettings LoadSettings();

        void AddOrSetSetting(string key, string value);

        string GetSetting(string key);

        void DeleteSetting(string key);
    }
}