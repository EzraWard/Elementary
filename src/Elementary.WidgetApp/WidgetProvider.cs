using Elementary.VerseOfTheDay.Interfaces;
using Elementary.VerseOfTheDay.Models;
using Elementary.WidgetApp.ComInfrastructure;
using Microsoft.Windows.Widgets.Providers;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Elementary.WidgetApp
{
    /// <summary>
    /// Implements the Windows 11 widget provider for the Elementary Verse of the Day widget.
    /// Registered as a COM out-of-process server in the package manifest.
    /// The CLSID must match the Class Id in Package.appxmanifest.
    /// </summary>
    [Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567891")]
    internal class WidgetProvider : IWidgetProvider
    {
        private static readonly string TemplatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "VerseOfTheDayWidgetTemplate.json");
        private readonly System.Collections.Generic.Dictionary<string, VotdImageSize> _widgetSizes = new System.Collections.Generic.Dictionary<string, VotdImageSize>(StringComparer.Ordinal);
        private const string FallbackTemplate = @"{
  ""type"": ""AdaptiveCard"",
  ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"",
  ""version"": ""1.5"",
  ""body"": [
    {
      ""type"": ""Image"",
      ""url"": ""${imageUri}"",
      ""size"": ""stretch"",
      ""altText"": ""Verse of the Day""
    },
    {
      ""type"": ""TextBlock"",
      ""text"": ""${reference}"",
      ""size"": ""small"",
      ""color"": ""light"",
      ""isSubtle"": true
    }
  ]
}";

        private readonly IVerseOfTheDayService _votdService;
        private readonly IVotdStorageService _storage;
        private readonly WidgetServerLifetime _lifetime;

        public WidgetProvider(
            IVerseOfTheDayService votdService,
            IVotdStorageService storage,
            WidgetServerLifetime lifetime)
        {
            _votdService = votdService;
            _storage = storage;
            _lifetime = lifetime;
        }

        // Called when a new widget instance is created.
        public void CreateWidget(WidgetContext widgetContext)
        {
            _lifetime.TrackWidget(widgetContext.Id);
            TrackWidgetSize(widgetContext);
            _ = UpdateWidgetAsync(widgetContext.Id);
        }

        // Called when a widget instance is deleted.
        public void DeleteWidget(string widgetId, string customState)
        {
            // Cache is shared across surfaces — nothing to clean up per-widget.
            _lifetime.UntrackWidget(widgetId);
            _widgetSizes.Remove(widgetId);
        }

        // Called when the widget is first created or re-activated.
        public void Activate(WidgetContext widgetContext)
        {
            _lifetime.TrackWidget(widgetContext.Id);
            TrackWidgetSize(widgetContext);
            _ = UpdateWidgetAsync(widgetContext.Id);
        }

        // Called when the widget host requests fresh data.
        public void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
        {
            _lifetime.KeepAlive();
            // Widget click — launch the UWP app via protocol activation.
            var uri = new Uri("elementary://");
            _ = Windows.System.Launcher.LaunchUriAsync(uri);
        }

        public void OnWidgetContextChanged(WidgetContextChangedArgs contextChangedArgs)
        {
            _lifetime.TrackWidget(contextChangedArgs.WidgetContext.Id);
            TrackWidgetSize(contextChangedArgs.WidgetContext);
            _ = UpdateWidgetAsync(contextChangedArgs.WidgetContext.Id);
        }

        public void Deactivate(string widgetId)
        {
            _lifetime.UntrackWidget(widgetId);
            _widgetSizes.Remove(widgetId);
        }

        public void OnCustomizationRequested(WidgetCustomizationRequestedArgs customizationRequestedArgs) { }

        private async Task UpdateWidgetAsync(string widgetId)
        {
            try
            {
                string dateKey = DateTime.UtcNow.ToString("yyyy-MM-dd");
                var imageSize = ResolveWidgetImageSize(widgetId);
                string filename = $"{dateKey}_{imageSize}.png";

                byte[]? imageBytes = null;
                VerseOfTheDayResult? result = null;

                if (!await _storage.ExistsAsync(filename))
                {
                    result = await _votdService.GetAsync(imageSize);
                    imageBytes = result.ImageBytes;
                    await _storage.SaveAsync(filename, imageBytes);
                }
                else
                {
                    imageBytes = await _storage.LoadAsync(filename);
                }

                // Convert bytes to a base64 data URI for the widget template
                var base64 = imageBytes != null ? Convert.ToBase64String(imageBytes) : string.Empty;
                var dataUri = $"data:image/png;base64,{base64}";

                string template = GetTemplate();
                string data = GetData(dataUri, result?.Reference ?? string.Empty);

                var updateOptions = new WidgetUpdateRequestOptions(widgetId)
                {
                    Template = template,
                    Data = data,
                    CustomState = dateKey
                };

                WidgetManager.GetDefault().UpdateWidget(updateOptions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WidgetProvider] UpdateWidgetAsync failed: {ex.Message}");
            }
        }

        private static string GetTemplate()
        {
            try
            {
                return File.ReadAllText(TemplatePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WidgetProvider] Failed to read widget template at '{TemplatePath}': {ex.Message}");
                return FallbackTemplate;
            }
        }

        private static string GetData(string imageUri, string reference)
            => $"{{\"imageUri\":\"{imageUri}\",\"reference\":\"{EscapeJson(reference)}\"}}";

        private static string EscapeJson(string s)
            => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        private void TrackWidgetSize(WidgetContext widgetContext)
        {
            _widgetSizes[widgetContext.Id] = ResolveWidgetImageSize(widgetContext);
        }

        private VotdImageSize ResolveWidgetImageSize(string widgetId)
        {
            return _widgetSizes.TryGetValue(widgetId, out var imageSize)
                ? imageSize
                : VotdImageSize.Widget640x360;
        }

        private static VotdImageSize ResolveWidgetImageSize(WidgetContext widgetContext)
        {
            var sizeName = widgetContext.GetType().GetProperty("Size")?.GetValue(widgetContext)?.ToString();
            if (string.Equals(sizeName, "Small", StringComparison.OrdinalIgnoreCase))
            {
                return VotdImageSize.TileMedium150;
            }

            if (string.Equals(sizeName, "Large", StringComparison.OrdinalIgnoreCase))
            {
                return VotdImageSize.TileLarge310x310;
            }

            return VotdImageSize.Widget640x360;
        }
    }
}
