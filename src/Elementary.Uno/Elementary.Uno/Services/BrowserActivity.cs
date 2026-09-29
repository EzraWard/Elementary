using System.Runtime.InteropServices.JavaScript;

namespace Elementary.Uno.Services;

internal static partial class BrowserActivity
{
    [JSImport("globalThis.document.hasFocus")]
    internal static partial bool HasFocus();
}
