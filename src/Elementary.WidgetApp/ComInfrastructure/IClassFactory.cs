using System;
using System.Runtime.InteropServices;

namespace Elementary.WidgetApp.ComInfrastructure
{
    [ComImport]
    [ComVisible(false)]
    [Guid("00000001-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IClassFactory
    {
        [PreserveSig]
        int CreateInstance(IntPtr outer, ref Guid iid, out IntPtr result);

        [PreserveSig]
        int LockServer(bool fLock);
    }
}
