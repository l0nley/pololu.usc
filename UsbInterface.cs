using Pololu.Usc.Interop;
using Pololu.Usc.Models;
using Pololu.Usc.Structs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pololu.Usc
{
    internal sealed class UsbInterface
    {
        private static readonly Lazy<UsbInterface> _instance = new Lazy<UsbInterface>(() => new UsbInterface());
        private UsbContext _context;


        private UsbInterface()
        {
        }

        private UsbContext Context
        {
            get
            {
                if(_context == null || _context.IsInvalid)
                {
                    Helpers.ThrowIfErrorCode(UsbInterop.Instance.Init(out _context));
                }

                return _context;
            }
        }

        public static UsbInterface Instance => _instance.Value;

        public unsafe IEnumerable<DeviceDescription> GetDeviceList(ushort vendorId, ushort[] productIdArray)
        {
            var list = new List<DeviceDescription>();

            int count = Helpers.ThrowIfErrorCode(UsbInterop.Instance.GetDeviceList(Context, out IntPtr* deviceList));
            try
            {
                for (int i = 0; i < count; i++)
                {
                    var device = deviceList[i];
                    foreach (ushort productId in productIdArray)
                    {
                        if (DeviceMatchesVendorProduct(device, vendorId, productId))
                        {
                            Helpers.ThrowIfErrorCode(UsbInterop.Instance.OpenDevice(device, out IntPtr deviceHandle));
                            var serialNumber = GetDeviceSerialNumber(deviceHandle);
                            list.Add(new DeviceDescription(device, "#" + serialNumber, serialNumber, productId));
                            UsbInterop.Instance.CloseDevice(device);
                        }
                    }
                }
            }
            finally
            {
                UsbInterop.Instance.FreeDeviceList(deviceList, 0);
            }

            return list;
        }

        private void HandleEvents()
        {
            Helpers.ThrowIfErrorCode(UsbInterop.Instance.HandleEvents(_context));
        }

        private DeviceDescriptor GetDeviceDescriptor(IntPtr deviceHandle)
        {
            Helpers.ThrowIfErrorCode(UsbInterop.Instance.GetDeviceDescriptor(deviceHandle, out DeviceDescriptor descriptor));
            return descriptor;
        }

        private unsafe string GetDeviceSerialNumber(IntPtr deviceHandle)
        {
            var descriptor = GetDeviceDescriptor(deviceHandle);
            var buffer = new byte[100];
            int length;
            fixed (byte* p = buffer)
            {
                length = Helpers.ThrowIfErrorCode(UsbInterop.Instance.GetStringDescriptorASCII(deviceHandle, descriptor.iSerialNumber, p, buffer.Length));
            }

            return Encoding.ASCII.GetString(buffer, 0, length);
        }

        private bool DeviceMatchesVendorProduct(IntPtr device, ushort idVendor, ushort idProduct)
        {
            var descriptor = UsbInterop.Instance.GetDeviceDescriptor(device, out DeviceDescriptor desc);
            return idVendor == desc.idVendor && idProduct == desc.idProduct;
        }
    }
}
