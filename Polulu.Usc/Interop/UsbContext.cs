using System;
using System.Runtime.InteropServices;

namespace Pololu.Usc.Interop
{
    internal class UsbContext : SafeHandle
    {
        private UsbContext() : base(IntPtr.Zero, true)
        {
        }

        public override bool IsInvalid
        {
            get
            {
                return (handle == IntPtr.Zero);
            }
        }

        override protected bool ReleaseHandle()
        {
            UsbInterop.Instance.Exit(handle);
            return true;
        }
    }
}