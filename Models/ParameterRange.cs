using System;

namespace Pololu.Usc.Models
{
    public class ParameterRange
    {
        public byte Bytes { get; }
        public int MinimumValue { get; }
        public int MaximumValue { get;  }

        internal ParameterRange(byte bytes, int minimumValue, int maximumValue)
        {
            Bytes = bytes;
            MinimumValue = minimumValue;
            MaximumValue = maximumValue;
        }

        public bool Signed
        {
            get
            {
                return MinimumValue < 0;
            }
        }

        internal static ParameterRange u32 = new ParameterRange(4, 0, 0x7FFFFFFF);
        internal static ParameterRange u16 = new ParameterRange(2, 0, 0xFFFF);
        internal static ParameterRange u12 = new ParameterRange(2, 0, 0x0FFF);
        internal static ParameterRange u10 = new ParameterRange(2, 0, 0x03FF);
        internal static ParameterRange u8 = new ParameterRange(1, 0, 0xFF);
        internal static ParameterRange u7 = new ParameterRange(1, 0, 0x7F);
        internal static ParameterRange boolean = new ParameterRange(1, 0, 1);

        internal void ThrowIfNotValid(ushort argumentValue, string name)
        {
            if (argumentValue < MinimumValue || argumentValue > MaximumValue)
            {
                throw new ArgumentException($"The {name} must be between {MinimumValue} and  {MaximumValue} but the value given was {argumentValue}");
            }
        }
    }
}
