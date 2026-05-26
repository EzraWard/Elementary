using Microsoft.Windows.Widgets.Providers;
using System;
using System.Runtime.InteropServices;
using WinRT;

namespace Elementary.WidgetApp.ComInfrastructure
{
    /// <summary>
    /// COM class factory that returns a pre-built IWidgetProvider singleton.
    /// This avoids requiring a parameterless constructor on the provider class.
    /// </summary>
    internal class WidgetProviderClassFactory : IClassFactory
    {
        private readonly IWidgetProvider _provider;

        public WidgetProviderClassFactory(IWidgetProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public int CreateInstance(IntPtr outer, ref Guid iid, out IntPtr result)
        {
            if (outer != IntPtr.Zero)
            {
                result = IntPtr.Zero;
                return NativeMethods.CLASS_E_NOAGGREGATION;
            }

            result = MarshalInspectable<IWidgetProvider>.FromManaged(_provider);
            return 0; // S_OK
        }

        public int LockServer(bool fLock) => 0; // S_OK
    }
}
