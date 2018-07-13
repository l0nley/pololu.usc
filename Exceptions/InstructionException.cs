using System;

namespace Pololu.Usc.Exceptions
{

    [Serializable]
    public class InstructionException : Exception
    {
        public InstructionException() { }
        public InstructionException(string message) : base(message) { }
        public InstructionException(string message, Exception inner) : base(message, inner) { }
        protected InstructionException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
