using System;
using Pololu.Usc.Structs;

namespace Pololu.Usc.Interop
{
    internal interface IUsbInterop
    {
        int Init(out UsbContext context);
        void Exit(IntPtr handle);
        int HandleEvents(UsbContext context);
        int GetDeviceDescriptor(IntPtr deviceHandle, out DeviceDescriptor descriptor);
        unsafe int GetStringDescriptorASCII(IntPtr deviceHandle, byte iSerialNumber, byte* p, int length);
        int OpenDevice(IntPtr devicePointer, out IntPtr deviceHandle);
        void CloseDevice(IntPtr deviceHandle);
        unsafe int GetDeviceList(UsbContext context, out IntPtr* device_list);
        unsafe void FreeDeviceList(IntPtr* deviceList, int v);
        void UnrefDevice(IntPtr item);
        unsafe int ControlTransfer(IntPtr deviceHandle, byte requestType, byte request, ushort value, ushort index, void* data, ushort length, ushort timeout);
    }
}
