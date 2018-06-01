using System;

namespace Pololu.Usc.Exceptions
{

    [Serializable]
    public class InvalidProductIdentityException : Exception
    {
        public InvalidProductIdentityException() { }
        public InvalidProductIdentityException(string message) : base(message) { }
        public InvalidProductIdentityException(string message, Exception inner) : base(message, inner) { }
        protected InvalidProductIdentityException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
