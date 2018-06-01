using Pololu.Usc.Structs;
using System;
using System.Runtime.InteropServices;

namespace Pololu.Usc.Interop
{
    internal class LinuxUsbInterop : IUsbInterop
    {
#pragma warning disable IDE1006 // Naming Styles
        [DllImport("libusb-1.0", EntryPoint = "libusb_init")]
        /// <summary>
        /// called to initialize the device context before any using any libusb functions
        /// </summary>
        /// <returns>an error code</returns>
        internal static extern int libusb_init(out UsbContext ctx);

        [DllImport("libusb-1.0", EntryPoint = "libusb_exit")]
        /// <summary>
        /// called with the context when closing
        /// </summary>
        internal static extern void libusb_exit(IntPtr ctx);


        [DllImport("libusb-1.0", EntryPoint = "libusb_get_device_descriptor")]
        internal static extern int libusb_get_device_descriptor(IntPtr device, out DeviceDescriptor deviceDescriptor);

        [DllImport("libusb-1.0")]
        internal static unsafe extern int libusb_handle_events(UsbContext ctx);


        [DllImport("libusb-1.0", EntryPoint = "libusb_get_device_list")]
        /// <summary>
        /// gets a list of device pointers - must be freed with libusbFreeDeviceList
        /// </summary>
        /// <returns>number of devices OR an error code</returns>
        internal static unsafe extern int libusb_get_device_list(UsbContext ctx, out IntPtr* list);

        [DllImport("libusb-1.0", EntryPoint = "libusb_free_device_list")]
        /// <summary>
        /// Frees a device list.  Decrements the reference count for each device by 1
        /// if the unref_devices parameter is set.
        /// </summary>
        internal static unsafe extern void libusb_free_device_list(IntPtr* list, int unrefDevices);


        [DllImport("libusb-1.0", EntryPoint = "libusb_unref_device")]
        /// <summary>
        /// Decrements the reference count on a device.
        /// </summary>
        internal static extern void libusb_unref_device(IntPtr device);

        [DllImport("libusb-1.0", EntryPoint = "libusb_get_string_descriptor_ascii")]
        /// <summary>
        /// Gets the simplest version of a string descriptor
        /// </summary>
        internal static unsafe extern int libusb_get_string_descriptor_ascii(IntPtr deviceHandle, byte index, byte* data, int length);

        [DllImport("libusb-1.0", EntryPoint = "libusb_open")]
        /// <summary>
        /// Gets a device handle for a device.  Must be closed with libusb_close.
        /// </summary>
        internal static extern int libusb_open(IntPtr device, out IntPtr deviceHandle);


        [DllImport("libusb-1.0", EntryPoint = "libusb_close")]
        /// <summary>
        /// Closes a device handle.
        /// </summary>
        internal static extern void libusb_close(IntPtr deviceHandle);

        [DllImport("libusb-1.0", EntryPoint = "libusb_get_device")]
        /// <summary>
        /// Gets the device from a device handle.
        /// </summary>
        internal static extern IntPtr libusb_get_device(IntPtr deviceHandle);

        [DllImport("libusb-1.0", EntryPoint = "libusb_control_transfer")]
        /// <returns>the number of bytes transferred or an error code</returns>
        internal static extern unsafe int libusb_control_transfer(IntPtr deviceHandle, byte requesttype,
                                               byte request, ushort value, ushort index,
                                               void* bytes, ushort size, uint timeout);


#pragma warning restore IDE1006 // Naming Styles


        public int Init(out UsbContext context)
        {
            return libusb_init(out context);
        }

        public void Exit(IntPtr handle)
        {
            libusb_exit(handle);
        }

        public int HandleEvents(UsbContext context)
        {
            return libusb_handle_events(context);
        }

        public int GetDeviceDescriptor(IntPtr deviceHandle, out DeviceDescriptor descriptor)
        {
            return libusb_get_device_descriptor(deviceHandle, out descriptor);
        }

        public unsafe int GetStringDescriptorASCII(IntPtr deviceHandle, byte iSerialNumber, byte* p, int length)
        {
            return libusb_get_string_descriptor_ascii(deviceHandle, iSerialNumber, p, length);
        }

        public int OpenDevice(IntPtr devicePointer, out IntPtr deviceHandle)
        {
            return libusb_open(devicePointer, out deviceHandle);
        }

        public void CloseDevice(IntPtr deviceHandle)
        {
            libusb_close(deviceHandle);
        }

        public unsafe int GetDeviceList(UsbContext context, out IntPtr* deviceList)
        {
            return libusb_get_device_list(context, out deviceList);
        }

        public unsafe void FreeDeviceList(IntPtr* deviceList, int v)
        {
            libusb_free_device_list(deviceList, v);
        }

        public void UnrefDevice(IntPtr device)
        {
            libusb_unref_device(device);
        }

        public unsafe int ControlTransfer(IntPtr deviceHandle, byte requestType, byte request, ushort value, ushort index, void* data, ushort length, ushort v)
        {
            return libusb_control_transfer(deviceHandle, requestType, request, value, index, data, length, v);
        }
    }
}
