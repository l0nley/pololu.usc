using Pololu.Usc.Models;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Pololu.Usc.Linux
{
    public abstract class UsbDevice : IDisposable
    {
        protected ushort GetProductID()
        {
            return LibUsb.GetDeviceDescriptor(DeviceHandle).idProduct;
        }

        /// <summary>
        /// Gets the serial number.
        /// </summary>
        public String GetSerialNumber()
        {
            return LibUsb.GetSerialNumber(DeviceHandle);
        }


        protected unsafe void ControlTransfer(byte RequestType, byte Request, ushort Value, ushort Index)
        {
            int ret = LibusbControlTransfer(DeviceHandle, RequestType, Request,
                                        Value, Index, (byte*)0, 0, (ushort)5000);
            LibUsb.ThrowIfError(ret); //"Control transfer failed");
        }

        protected unsafe uint ControlTransfer(byte RequestType, byte Request, ushort Value, ushort Index, byte[] data)
        {
            fixed (byte* pointer = data)
            {
                return ControlTransfer(RequestType, Request,
                                            Value, Index, pointer, (ushort)data.Length);
            }
        }

        protected unsafe uint ControlTransfer(byte RequestType, byte Request, ushort Value, ushort Index, void* data, ushort length)
        {
            int ret = LibusbControlTransfer(DeviceHandle, RequestType, Request,
                                        Value, Index, data, length, (ushort)5000);
            LibUsb.ThrowIfError(ret); //,"Control transfer failed");
            return (uint)ret;
        }

        readonly IntPtr privateDeviceHandle;

        internal IntPtr DeviceHandle
        {
            get { return privateDeviceHandle; }
        }

        /// <summary>
        /// Create a usb device from a deviceListItem
        /// </summary>
        /// <param name="handles"></param>
        protected UsbDevice(DeviceListItem deviceListItem)
        {
            LibUsb.ThrowIfError(LibusbOpen(deviceListItem.DevicePointer, out privateDeviceHandle));
        }

        /// <summary>
        /// disconnects from the usb device.  This is the same as Dispose().
        /// </summary>
        public void Disconnect()
        {
            LibusbClose(DeviceHandle);
        }

        /// <summary>
        /// Disconnects from the USB device, freeing all resources
        /// that were allocated when the connection was made.
        /// This is the same as disconnect().
        /// </summary>
        public void Dispose()
        {
            Disconnect();
        }

        [DllImport("libusb-1.0", EntryPoint = "libusb_control_transfer")]
        /// <returns>the number of bytes transferred or an error code</returns>
        static extern unsafe int LibusbControlTransfer(IntPtr device_handle, byte requesttype,
                                                byte request, ushort value, ushort index,
                                                void* bytes, ushort size, uint timeout);

        [DllImport("libusb-1.0", EntryPoint = "libusb_get_device_descriptor")]
        internal static extern int LibusbGetDeviceDescriptor(IntPtr device, out LibusbDeviceDescriptor device_descriptor);

        [DllImport("libusb-1.0", EntryPoint = "libusb_init")]
        /// <summary>
        /// called to initialize the device context before any using any libusb functions
        /// </summary>
        /// <returns>an error code</returns>
        internal static extern int LibusbInit(out LibusbContext ctx);

        [DllImport("libusb-1.0", EntryPoint = "libusb_get_device_list")]
        /// <summary>
        /// gets a list of device pointers - must be freed with libusbFreeDeviceList
        /// </summary>
        /// <returns>number of devices OR an error code</returns>
        internal static unsafe extern int LibusbGetDeviceList(LibusbContext ctx, out IntPtr* list);

        [DllImport("libusb-1.0", EntryPoint = "libusb_free_device_list")]
        /// <summary>
        /// Frees a device list.  Decrements the reference count for each device by 1
        /// if the unref_devices parameter is set.
        /// </summary>
        internal static unsafe extern void LibusbFreeDeviceList(IntPtr* list, int unref_devices);

        [DllImport("libusb-1.0", EntryPoint = "libusb_unref_device")]
        /// <summary>
        /// Decrements the reference count on a device.
        /// </summary>
        internal static extern void LibusbUnrefDevice(IntPtr device);

        [DllImport("libusb-1.0", EntryPoint = "libusb_get_string_descriptor_ascii")]
        /// <summary>
        /// Gets the simplest version of a string descriptor
        /// </summary>
        internal static unsafe extern int LibusbGetStringDescriptorASCII(IntPtr device_handle, byte index, byte* data, int length);

        [DllImport("libusb-1.0", EntryPoint = "libusb_open")]
        /// <summary>
        /// Gets a device handle for a device.  Must be closed with libusb_close.
        /// </summary>
        internal static extern int LibusbOpen(IntPtr device, out IntPtr device_handle);

        [DllImport("libusb-1.0", EntryPoint = "libusb_close")]
        /// <summary>
        /// Closes a device handle.
        /// </summary>
        internal static extern void LibusbClose(IntPtr device_handle);

        [DllImport("libusb-1.0", EntryPoint = "libusb_get_device")]
        /// <summary>
        /// Gets the device from a device handle.
        /// </summary>
        internal static extern IntPtr LibusbGetDevice(IntPtr device_handle);

        /// <summary>
        /// true if the devices are the same
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool IsSameDeviceAs(DeviceListItem item)
        {
            return (LibusbGetDevice(DeviceHandle) == item.DevicePointer);
        }

        /// <summary>
        /// gets a list of devices
        /// </summary>
        /// <returns></returns>
        protected static unsafe List<DeviceListItem> GetDeviceList(Guid deviceInterfaceGuid)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// gets a list of devices by vendor and product ID
        /// </summary>
        /// <returns></returns>
        protected static unsafe List<DeviceListItem> GetDeviceList(UInt16 vendorId, UInt16[] productIdArray)
        {
            var list = new List<DeviceListItem>();

            int count = LibUsb.ThrowIfError(UsbDevice.LibusbGetDeviceList(LibUsb.Context, out IntPtr* device_list));

            int i;
            for (i = 0; i < count; i++)
            {
                IntPtr device = device_list[i];

                foreach (UInt16 productId in productIdArray)
                {
                    if (LibUsb.DeviceMatchesVendorProduct(device, vendorId, productId))
                    {
                        LibUsb.ThrowIfError(UsbDevice.LibusbOpen(device, out IntPtr device_handle));
                        //                    "Error connecting to device to get serial number ("+(i+1)+" of "+count+", "+device.ToString("x8")+").");

                        string serialNumber = LibUsb.GetSerialNumber(device_handle);
                        list.Add(new DeviceListItem(device, "#" + serialNumber, serialNumber, productId));

                        UsbDevice.LibusbClose(device_handle);
                    }
                }
            }


            // Free device list without unreferencing.
            // Unreference/free the individual devices in the
            // DeviceListItem destructor.
            UsbDevice.LibusbFreeDeviceList(device_list, 0);

            return list;
        }

        //protected AsynchronousInTransfer newAsynchronousInTransfer(byte endpoint, uint size, uint timeout)
        //{
        //    return new AsynchronousInTransfer(this, endpoint, size, timeout);
        //}
    }
}