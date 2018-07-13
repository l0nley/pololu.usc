using Pololu.Usc.Enums;

namespace Pololu.Usc.Models
{
    public class ChannelSetting
    {
        /// <summary>
        /// Name.  The Usc class stores this in the registry, not the device.
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Type (servo, output, input).
        /// </summary>
        public ChannelMode Mode { get; set; } = ChannelMode.Servo;

        /// <summary>
        /// HomeType (off, ignore, goto).
        /// </summary>
        public HomeMode HomeMode = HomeMode.Off;

        /// <summary>
        /// Home position: the place to go on startup.
        /// If type==servo, units are 0.25 us (qus).
        /// If type==output, the threshold between high and low is 1500.
        /// 
        /// This value is only saved on the device if homeType == Goto.
        /// </summary>
        public ushort Home { get; set; } = 6000;

        /// <summary>
        /// Minimum (units of 0.25 us, but stored on the device in units of 16 us).
        /// </summary>
        public ushort Minimum { get; set;} = 3968;

        /// <summary>
        /// Maximum (units of 0.25 us, but stored on the device in units of 16 us).
        /// </summary>
        public ushort Maximum { get; set; } = 8000;

        /// <summary>
        /// Neutral: the center of the 8-bit set target command (value at 127).
        /// If type==servo, units are 0.25 us (qus).
        /// If type==output, the threshold between high and low is 1500.
        /// </summary>
        public ushort Neutral { get; set; } = 6000;

        /// <summary>
        /// Range: the +/- extent of the 8-bit command.
        ///   8-bit(254) = neutral + range,
        ///   8-bit(0) = neutral - range
        /// If type==servo units are 0.25 us (qus) (but stored on device in
        /// units of 127*0.25us = 31.75 us.
        /// Range = 0-127*255 = 0-32385 qus.
        /// Increment = 127 qus
        /// </summary>
        public ushort Range { get; set; } = 1905;

        /// <summary>
        /// Speed: the maximum change in position (qus) per update.  0 means no limit.
        /// Units depend on your settings.
        /// Stored on device in this format: [0-31]*2^[0-7]
        /// Range = 0-31*2^7 = 0-3968.
        /// Increment = 1.
        /// 
        /// Note that the *current speed* is stored on the device in units
        /// of qus, and so it is not subject to the restrictions above!
        /// It can be any value 0-65535.
        /// </summary>
        public ushort Speed { get; set; }  = 0;

        /// <summary>
        /// Acceleration: the max change in speed every 80 ms.  0 means no limit.
        /// Units depend on your settings.
        /// Range = 0-255.
        /// Increment = 1.
        /// </summary>
        public byte Acceleration { get; set; } = 0;
    }
}