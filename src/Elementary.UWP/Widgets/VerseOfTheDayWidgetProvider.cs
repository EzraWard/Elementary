using Elementary.Core.Interfaces;
using Elementary.Widgets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Windows.Widgets.Providers;
using System;

namespace Elementary.Widgets
{
    /// <summary>
    /// Widget provider for the Verse of the Day Windows 11 widget.
    /// Registered as a COM activatable class in Package.appxmanifest.
    /// Activated by the widget host via the windows.widgetProvider extension.
    /// </summary>
    public sealed class VerseOfTheDayWidgetProvider : IWidgetProvider
    {
        public const string WidgetDefinitionId = "VerseOfTheDayWidget";

        private readonly IVerseOfTheDayService _verseOfTheDayService;

        public VerseOfTheDayWidgetProvider()
        {
            _verseOfTheDayService = App.Services.GetRequiredService<IVerseOfTheDayService>();
        }

        public VerseOfTheDayWidgetProvider(IVerseOfTheDayService verseOfTheDayService)
        {
            _verseOfTheDayService = verseOfTheDayService ?? throw new ArgumentNullException(nameof(verseOfTheDayService));
        }

        /// <inheritdoc/>
        public void Activate(WidgetContext widgetContext)
        {
            SendWidgetUpdate(widgetContext);
        }

        /// <inheritdoc/>
        public void Deactivate(string widgetId)
        {
            // No active resources to release when the widget is deactivated.
        }

        /// <inheritdoc/>
        public void OnWidgetContextChanged(WidgetContextChangedArgs contextChangedArgs)
        {
            SendWidgetUpdate(contextChangedArgs.WidgetContext);
        }

        /// <inheritdoc/>
        public void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
        {
            // The Verse of the Day widget has no interactive actions.
        }

        /// <inheritdoc/>
        public void OnWidgetCustomizationRequested(WidgetCustomizationRequestedArgs customizationRequestedArgs)
        {
            // No customization options are offered.
        }

        private void SendWidgetUpdate(WidgetContext widgetContext)
        {
            var verse = _verseOfTheDayService.GetVerseOfTheDay();
            var template = GetTemplate(widgetContext.Size);
            var data = WidgetDataBuilder.BuildAdaptiveCardData(verse);

            var updateOptions = new WidgetUpdateRequestOptions(widgetContext.Id)
            {
                Template = template,
                Data = data
            };

            WidgetManager.GetDefault().UpdateWidget(updateOptions);
        }

        private static string GetTemplate(WidgetSize size)
        {
            return size switch
            {
                WidgetSize.Small => SmallTemplate,
                WidgetSize.Large => LargeTemplate,
                _ => MediumTemplate
            };
        }

        // ----------------------------------------------------------------
        // Adaptive Card templates (one per supported widget size).
        // The ${...} tokens are resolved by the widget host at render time
        // against the data JSON produced by WidgetDataBuilder.
        // ----------------------------------------------------------------

        private const string SmallTemplate =
            "{" +
            "\"$schema\":\"http://adaptivecards.io/schemas/adaptive-card.json\"," +
            "\"type\":\"AdaptiveCard\"," +
            "\"version\":\"1.6\"," +
            "\"body\":[" +
            "{\"type\":\"Image\",\"url\":\"${imageUrl}\",\"size\":\"stretch\",\"horizontalAlignment\":\"center\"}" +
            "]" +
            "}";

        private const string MediumTemplate =
            "{" +
            "\"$schema\":\"http://adaptivecards.io/schemas/adaptive-card.json\"," +
            "\"type\":\"AdaptiveCard\"," +
            "\"version\":\"1.6\"," +
            "\"body\":[" +
            "{\"type\":\"TextBlock\",\"text\":\"${title}\",\"size\":\"small\",\"weight\":\"bolder\",\"wrap\":true}," +
            "{\"type\":\"Image\",\"url\":\"${imageUrl}\",\"size\":\"stretch\",\"horizontalAlignment\":\"center\"}" +
            "]" +
            "}";

        private const string LargeTemplate =
            "{" +
            "\"$schema\":\"http://adaptivecards.io/schemas/adaptive-card.json\"," +
            "\"type\":\"AdaptiveCard\"," +
            "\"version\":\"1.6\"," +
            "\"body\":[" +
            "{\"type\":\"TextBlock\",\"text\":\"${title}\",\"size\":\"medium\",\"weight\":\"bolder\",\"wrap\":true}," +
            "{\"type\":\"Image\",\"url\":\"${imageUrl}\",\"size\":\"stretch\",\"horizontalAlignment\":\"center\"}" +
            "]" +
            "}";
    }
}
