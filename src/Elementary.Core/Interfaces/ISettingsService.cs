namespace Elementary.Core.Interfaces
{
    public interface ISettingsService
    {
        ISettings GetSettings();

        void SaveSettings(ISettings settings);
    }
}