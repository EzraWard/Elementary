using System.Runtime.InteropServices.JavaScript;

namespace Elementary.Uno.Services;

internal static partial class BrowserNavigationGesture
{
    [JSImport("globalThis.elementaryIsIosStandalone")]
    internal static partial bool IsIosStandalone();

    [JSImport("globalThis.elementarySetLeftEdgeSwipeHandler")]
    internal static partial void SetLeftEdgeSwipeHandler([JSMarshalAs<JSType.Function>] Action? handler);
}
