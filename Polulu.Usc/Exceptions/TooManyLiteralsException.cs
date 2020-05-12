using System;

namespace Pololu.Usc.Exceptions
{

    [Serializable]
    public class TooManyLiteralsException : Exception
    {
        public TooManyLiteralsException() { }
        public TooManyLiteralsException(string message) : base(message) { }
        public TooManyLiteralsException(string message, Exception inner) : base(message, inner) { }
        protected TooManyLiteralsException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
