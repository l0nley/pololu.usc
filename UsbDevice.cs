using Pololu.Usc.Enums;
using Pololu.Usc.Interop;
using Pololu.Usc.Models;
using System;
using System.Collections.Generic;

namespace Pololu.Usc
{
    internal class UsbDevice : IDisposable
    {
        private IntPtr _deviceHandle;

        public DeviceDescription Description { get; private set; }

        private bool _disposed;

        public ushort ControlTransferTimeout { get; set; } = 5000;

        public UsbDevice(DeviceDescription item)
        {
            Description = item;
        }

        public void Open()
        {
            ThrowIfDisposed();
            UsbInterop.Instance.OpenDevice(Description.DevicePointer, out _deviceHandle);
        }

        public unsafe int ControlTransfer(RequestType requestType, Request request, ushort value, ushort index)
        {
            ThrowIfDisposed();
            return Helpers.ThrowIfErrorCode(UsbInterop.Instance.ControlTransfer(_deviceHandle, (byte)requestType, (byte)request, value, index, (byte*)0, 0, ControlTransferTimeout));
        }


        public unsafe int ControlTransfer(RequestType requestType, Request request, ushort value, ushort index, void* data, ushort length)
        {
            ThrowIfDisposed();
            return Helpers.ThrowIfErrorCode(UsbInterop.Instance.ControlTransfer(_deviceHandle, (byte)requestType, (byte)request, value, index, data, length, ControlTransferTimeout));
        }

        public unsafe int ControlTransfer(RequestType requestType, Request request, ushort value, ushort index, byte[] data)
        {
            ThrowIfDisposed();
            fixed (byte* pointer = data)
            {
                return ControlTransfer(requestType, request, value, index, pointer, (ushort)data.Length);
            }
        }

        public void Dispose()
        {
            if (_deviceHandle != IntPtr.Zero)
            {
                UsbInterop.Instance.CloseDevice(_deviceHandle);
            }
            _disposed = true;
        }

        public override bool Equals(object obj)
        {
            return obj is UsbDevice device && EqualityComparer<IntPtr>.Default.Equals(_deviceHandle, device._deviceHandle);
        }

        public override int GetHashCode()
        {
            return -666568826 + EqualityComparer<IntPtr>.Default.GetHashCode(_deviceHandle);
        }

        private void ThrowIfDisposed()
        {
            if(_disposed)
            {
                throw new ObjectDisposedException(Description.Name);
            }
        }

    }
}
