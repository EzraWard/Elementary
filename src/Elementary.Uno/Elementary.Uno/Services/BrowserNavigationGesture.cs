using System.Runtime.InteropServices.JavaScript;

namespace Elementary.Uno.Services;

internal static partial class BrowserNavigationGesture
{
    [JSImport("globalThis.elementaryIsIos")]
    internal static partial bool IsIos();

    [JSImport("globalThis.elementarySetLeftEdgeSwipeHandler")]
    internal static partial void SetLeftEdgeSwipeHandler([JSMarshalAs<JSType.Function>] Action? handler);
}
