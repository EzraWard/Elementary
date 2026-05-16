using System;
using System.Runtime.InteropServices;

namespace Elementary.WidgetApp.ComInfrastructure
{
    internal static class NativeMethods
    {
        public const int CLASS_E_NOAGGREGATION = unchecked((int)0x80040110);

        [DllImport("ole32.dll")]
        public static extern int CoRegisterClassObject(
            ref Guid rclsid,
            [MarshalAs(UnmanagedType.Interface)] IClassFactory pUnk,
            [MarshalAs(UnmanagedType.U4)] CLSCTX dwClsContext,
            [MarshalAs(UnmanagedType.U4)] REGCLS flags,
            [Out, MarshalAs(UnmanagedType.U4)] out int lpdwRegister);

        [DllImport("ole32.dll")]
        [return: MarshalAs(UnmanagedType.U4)]
        public static extern int CoRevokeClassObject([MarshalAs(UnmanagedType.U4)] int dwRegister);

        [DllImport("ole32.dll")]
        public static extern int CoWaitForMultipleObjects(
            CWMO_FLAGS dwFlags, uint dwTimeout,
            int cHandles, IntPtr[] pHandles, out uint lpdwindex);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateEvent(
            IntPtr eventAttributes,
            [MarshalAs(UnmanagedType.Bool)] bool manualReset,
            [MarshalAs(UnmanagedType.Bool)] bool initialState,
            IntPtr name);

        public enum CLSCTX : int
        {
            LOCAL_SERVER = 0x4,
        }

        [Flags]
        public enum CWMO_FLAGS : int
        {
            CWMO_DISPATCH_CALLS = 1,
            CWMO_DISPATCH_WINDOW_MESSAGES = 2,
        }

        [Flags]
        public enum REGCLS : int
        {
            MULTIPLEUSE = 1,
        }
    }
}
