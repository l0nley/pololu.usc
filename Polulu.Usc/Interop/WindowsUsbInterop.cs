using Pololu.Usc.Structs;
using System;

namespace Pololu.Usc.Interop
{
    internal class WindowsUsbInterop : IUsbInterop
    {
        public void CloseDevice(IntPtr deviceHandle)
        {
            throw new NotImplementedException();
        }

        public unsafe int ControlTransfer(IntPtr deviceHandle, byte requestType, byte request, ushort value, ushort index, void* data, ushort length, ushort timeout)
        {
            throw new NotImplementedException();
        }

        public void Exit(IntPtr handle)
        {
            throw new NotImplementedException();
        }

        public unsafe void FreeDeviceList(IntPtr* deviceList, int v)
        {
            throw new NotImplementedException();
        }

        public int GetDeviceDescriptor(IntPtr deviceHandle, out DeviceDescriptor descriptor)
        {
            throw new NotImplementedException();
        }

        public unsafe int GetDeviceList(UsbContext context, out IntPtr* device_list)
        {
            throw new NotImplementedException();
        }

        public unsafe int GetStringDescriptorASCII(IntPtr deviceHandle, byte iSerialNumber, byte* p, int length)
        {
            throw new NotImplementedException();
        }

        public int HandleEvents(UsbContext context)
        {
            throw new NotImplementedException();
        }

        public int Init(out UsbContext context)
        {
            throw new NotImplementedException();
        }

        public int OpenDevice(IntPtr devicePointer, out IntPtr deviceHandle)
        {
            throw new NotImplementedException();
        }

        public void UnrefDevice(IntPtr item)
        {
            throw new NotImplementedException();
        }
    }
}
