using Pololu.Usc.Linux;
using System;
using System.Runtime.InteropServices;

namespace Pololu.Usc.Linux
{
    internal class LibusbContext : SafeHandle
    {
        private LibusbContext() : base(IntPtr.Zero, true)
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
            UsbInterop.libusbExit(handle);
            return true;
        }
    }
}