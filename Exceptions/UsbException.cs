using System;

namespace Pololu.Usc.Exceptions
{

    [Serializable]
    public class UsbException : Exception
    {
        public UsbException() { }
        public UsbException(string message) : base(message) { }
        public UsbException(string message, Exception inner) : base(message, inner) { }
        protected UsbException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
