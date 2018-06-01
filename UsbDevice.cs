using Pololu.Usc.Interop;
using Pololu.Usc.Models;
using System;
using System.Collections.Generic;

namespace Pololu.Usc
{
    public class UsbDevice : IDisposable
    {
        private IntPtr _deviceHandle;

        public DeviceDescription Description { get; private set; }

        public UsbDevice(DeviceDescription item)
        {
            Description = item;
        }

        public void Open()
        {
            UsbInterop.Instance.OpenDevice(Description.DevicePointer, out _deviceHandle);
        }

        public void Dispose()
        {
            if (_deviceHandle != IntPtr.Zero)
            {
                UsbInterop.Instance.CloseDevice(_deviceHandle);
            }
        }

        public override bool Equals(object obj)
        {
            return obj is UsbDevice device && EqualityComparer<IntPtr>.Default.Equals(_deviceHandle, device._deviceHandle);
        }

        public override int GetHashCode()
        {
            return -666568826 + EqualityComparer<IntPtr>.Default.GetHashCode(_deviceHandle);
        }

    }
}
