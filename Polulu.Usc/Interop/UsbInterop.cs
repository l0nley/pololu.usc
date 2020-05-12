using System;
using System.Runtime.InteropServices;

namespace Pololu.Usc.Interop
{
    internal sealed class UsbInterop
    {
        private static readonly Lazy<IUsbInterop> _instance = new Lazy<IUsbInterop>(() =>
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                return new LinuxUsbInterop();
            }
            throw new NotSupportedException("Only Linux platform supported for now");
        });

        public static IUsbInterop Instance => _instance.Value;

        private UsbInterop()
        {
        }

    }
}
