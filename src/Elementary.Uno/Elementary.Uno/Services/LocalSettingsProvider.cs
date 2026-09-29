using Elementary.Core.Interfaces;
using System.Runtime.InteropServices.JavaScript;

namespace Elementary.Uno.Services;

/// <summary>Persists the shared settings, history, and streak data in this browser's app storage.</summary>
internal sealed partial class LocalSettingsProvider : ISettingsProvider
{
    private const string Prefix = "Elementary.";
    public string GetSetting(string key) => Read(Prefix + key) ?? string.Empty;

    public void SaveSetting(string key, string value) => Write(Prefix + key, value);

    public void DeleteSetting(string key) => Remove(Prefix + key);

    [JSImport("globalThis.localStorage.getItem")]
    private static partial string? Read(string key);

    [JSImport("globalThis.localStorage.setItem")]
    private static partial void Write(string key, string value);

    [JSImport("globalThis.localStorage.removeItem")]
    private static partial void Remove(string key);
}
