using System;
using System.Linq;

namespace Pololu.Usc.Linux
{
    internal static class LibUsb
    {
        /// <summary>
        /// Do not use directly.  The property below initializes this
        /// with libusbInit when it is first used.
        /// </summary>
        private static LibusbContext _context;

        /// <summary>
        /// Raises an exception if its argument is negative, with a
        /// message describing which LIBUSB_ERROR it is.
        /// </summary>
        /// <returns>the code, if it is non-negative</returns>
        internal static int ThrowIfError(int code)
        {
            if (code >= 0)
                return code;

            throw new Exception(ErrorDescription(code));
        }

        internal static string ErrorDescription(int error)
        {
            switch (error)
            {
                case -1:
                    return "I/O error.";
                case -2:
                    return "Invalid parameter.";
                case -3:
                    return "Access denied.";
                case -4:
                    return "Device does not exist.";
                case -5:
                    return "No such entity.";
                case -6:
                    return "Busy.";
                case -7:
                    return "Timeout.";
                case -8:
                    return "Overflow.";
                case -9:
                    return "Pipe error.";
                case -10:
                    return "System call was interrupted.";
                case -11:
                    return "Out of memory.";
                case -12:
                    return "Unsupported/unimplemented operation.";
                case -99:
                    return "Other error.";
                default:
                    return "Unknown error code " + error + ".";
            };
        }

        internal static LibusbContext Context
        {
            get
            {
                if (_context == null || _context.IsInvalid)
                {
                    ThrowIfError(UsbDevice.LibusbInit(out _context));
                }
                return _context;
            }
        }

        internal static void HandleEvents()
        {
            ThrowIfError(UsbInterop.libusb_handle_events(_context));
        }

        /// <returns>the serial number</returns>
        internal static unsafe string GetSerialNumber(IntPtr deviceHandle)
        {
            var descriptor = GetDeviceDescriptor(deviceHandle);
            var buffer = new byte[100];
            int length;
            fixed (byte* p = buffer)
            {
                length = ThrowIfError(UsbDevice.LibusbGetStringDescriptorASCII(deviceHandle, descriptor.iSerialNumber, p, buffer.Length));
            }

            return new string(buffer.Take(length).Select(_ => (char)_).ToArray());
        }

        /// <returns>true iff the vendor and product ids match the device</returns>
        internal static bool DeviceMatchesVendorProduct(IntPtr device, ushort idVendor, ushort idProduct)
        {
            var descriptor = GetDeviceDescriptorFromDevice(device);
            return idVendor == descriptor.idVendor && idProduct == descriptor.idProduct;
        }

        /// <returns>the device descriptor</returns>
        internal static LibusbDeviceDescriptor GetDeviceDescriptor(IntPtr device_handle)
        {
            return GetDeviceDescriptorFromDevice(UsbDevice.LibusbGetDevice(device_handle));
        }

        /// <returns>the device descriptor</returns>
        static LibusbDeviceDescriptor GetDeviceDescriptorFromDevice(IntPtr device)
        {
            ThrowIfError(UsbDevice.LibusbGetDeviceDescriptor(device, out LibusbDeviceDescriptor descriptor));
            return descriptor;
        }
    }
}