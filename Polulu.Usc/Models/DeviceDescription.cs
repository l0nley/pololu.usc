using Pololu.Usc.Interop;
using System;

namespace Pololu.Usc.Models
{
    /// <summary>
    /// A class that represents a device connected to the computer.  This
    /// class can be used as an item in the device list dropdown box.
    /// </summary>
    public class DeviceDescription
    {
        /// <summary>
        /// The text to display to the user in the list to represent this
        /// device.  By default, this text is "#" + serialNumberString,
        /// but it can be changed to suit the application's needs
        /// (for example, adding model information to it).
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the serial number.
        /// </summary>
        public string SerialNumber { get; private set; }

        /// <summary>
        /// Gets the USB product ID of the device.
        /// </summary>
        public ushort ProductId { get; private set; }

        internal readonly IntPtr DevicePointer;

        internal DeviceDescription(IntPtr devicePointer, string text, string serialNumber, ushort productId)
        {
            DevicePointer = devicePointer;
            Name = text;
            SerialNumber = serialNumber;
            ProductId = productId;
        }

        ~DeviceDescription()
        {
            if(DevicePointer != IntPtr.Zero)
            {
                UsbInterop.Instance.UnrefDevice(DevicePointer);
            }
        }
    }
}