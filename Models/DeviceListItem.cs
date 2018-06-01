using Pololu.Usc.Linux;
using System;

namespace Pololu.Usc.Models
{
    /// <summary>
    /// A class that represents a device connected to the computer.  This
    /// class can be used as an item in the device list dropdown box.
    /// </summary>
    public class DeviceListItem
    {
        /// <summary>
        /// The text to display to the user in the list to represent this
        /// device.  By default, this text is "#" + serialNumberString,
        /// but it can be changed to suit the application's needs
        /// (for example, adding model information to it).
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets the serial number.
        /// </summary>
        public string SerialNumber { get; private set; }

        /// <summary>
        /// Gets the device pointer.
        /// </summary>
        internal IntPtr DevicePointer { get; private set; }

        /// <summary>
        /// Gets the USB product ID of the device.
        /// </summary>
        public ushort ProductId { get; private set; }

        /// <summary>
        /// true if the devices are the same
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool IsSameDeviceAs(DeviceListItem item)
        {
            return (DevicePointer == item.DevicePointer);
        }

        internal DeviceListItem(IntPtr devicePointer, string text, string serialNumber, UInt16 productId)
        {
            DevicePointer = devicePointer;
            Text = text;
            SerialNumber = serialNumber;
            ProductId = productId;
        }

        ~DeviceListItem()
        {
            if (DevicePointer != IntPtr.Zero)
                UsbDevice.LibusbUnrefDevice(DevicePointer);
        }
    }
}