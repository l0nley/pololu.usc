using System.Runtime.InteropServices;

namespace Pololu.Usc.Structs
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ServoStatus
    {
        /// <summary>The position in units of quarter-microseconds.</summary>
        public ushort Position;

        /// <summary>The target position in units of quarter-microseconds.</summary>
        public ushort Target;

        /// <summary>The speed limit.  Units depends on your settings.</summary>
        public ushort Speed;

        /// <summary>The acceleration limit.  Units depend on your settings.</summary>
        public byte Acceleration;
    };
}