#if !WINDOWS
using System.Runtime.InteropServices.JavaScript;

namespace Elementary.Uno.Services;

internal static partial class BrowserActivity
{
    [JSImport("globalThis.elementaryPrefersReducedMotion")]
    internal static partial bool PrefersReducedMotion();

    [JSImport("globalThis.document.hasFocus")]
    internal static partial bool HasFocus();
}
#endif
