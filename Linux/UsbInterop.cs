using System;
using System.Runtime.InteropServices;

namespace Pololu.Usc.Linux
{
#pragma warning disable IDE1006 // Naming Styles
    public static class UsbInterop
    {
        [DllImport("libusb-1.0")]

        internal static unsafe extern int libusb_handle_events(LibusbContext ctx);


        [DllImport("libusb-1.0", EntryPoint = "libusb_exit")]
        /// <summary>
        /// called with the context when closing
        /// </summary>
        internal static extern void libusbExit(IntPtr ctx);

    }
#pragma warning restore IDE1006 // Naming Styles
}
