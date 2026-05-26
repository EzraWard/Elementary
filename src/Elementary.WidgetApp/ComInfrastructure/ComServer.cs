using System;
using System.Threading;

namespace Elementary.WidgetApp.ComInfrastructure
{
    /// <summary>
    /// Registers a COM class factory and blocks until the widget host is done with the server.
    /// </summary>
    internal static class ComServer
    {
        public static void Run(Guid clsid, IClassFactory factory, WaitHandle shutdownSignal)
        {
            int hResult = NativeMethods.CoRegisterClassObject(
                ref clsid,
                factory,
                NativeMethods.CLSCTX.LOCAL_SERVER,
                NativeMethods.REGCLS.MULTIPLEUSE,
                out int cookie);

            if (hResult != 0)
                throw new ApplicationException($"CoRegisterClassObject failed: 0x{hResult:X}");

            NativeMethods.CoWaitForMultipleObjects(
                NativeMethods.CWMO_FLAGS.CWMO_DISPATCH_CALLS | NativeMethods.CWMO_FLAGS.CWMO_DISPATCH_WINDOW_MESSAGES,
                0xFFFFFFFF,
                1,
                new[] { shutdownSignal.SafeWaitHandle.DangerousGetHandle() },
                out _);

            NativeMethods.CoRevokeClassObject(cookie);
        }
    }
}
