using Pololu.Usc.Models;
using System;
using System.Collections.Generic;

namespace Pololu.Usc.Linux
{
    public abstract class OldUsbDevice : IDisposable
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

       
        /// <summary>
        /// gets a list of devices by vendor and product ID
        /// </summary>
        /// <returns></returns>
        

        //protected AsynchronousInTransfer newAsynchronousInTransfer(byte endpoint, uint size, uint timeout)
        //{
        //    return new AsynchronousInTransfer(this, endpoint, size, timeout);
        //}
    }
}