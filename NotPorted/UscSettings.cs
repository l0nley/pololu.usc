using Pololu.Usc.Enums;
using Pololu.Usc.Linux;
using System.Collections.Generic;

namespace Pololu.Usc.Models
{
    /*
    public class UscSettings
    {
        /// <summary>
        /// The number of servo ports available (0-5).  This, along with the
        /// servoPeriod, determine the "maximum maximum pulse width".
        /// </summary>
        public byte ServosAvailable { get; set; } = 6;

        /// <summary>
        /// This setting only applies to the Micro Maestro.
        /// For the Mini Maestro, see miniMaestroServoPeriod.
        /// 
        /// The total time allotted to each servo channel, in units of
        /// 256/12 = 21.33333 us.  The unit for this one are unusual, because
        /// that is the way it is stored on the device and its unit is not
        /// a multiple of 4, so we would have inevitable rounding errors if we
        /// tried to represent it in quarter-microseconds.
        /// 
        /// Default is 156, so with 6 servos available you get ~20ms between
        /// pulses on a given channel.
        /// </summary>
        public byte ServoPeriod { get; set; } = 156;

        /// <summary>
        /// This setting only applies to the Mini Maestro.
        /// For the Micro Maestro, see microMaestroServoPeriod.
        /// 
        /// The length of the time period in which the Mini Maestro sends pulses
        /// to all the enabled servos, in units of quarter microseconds.
        /// 
        /// Valid values for this parameter are 0 to 16,777,215.  But 
        /// 
        /// Default is 80000, so each servo receives a pulse every 20 ms (50 Hz).
        /// </summary>
        public uint MiniMaestroServoPeriod { get; set; } = 80000;

        /// <summary>
        /// This setting only applied to the Mini Maestro.
        /// The non-multiplied servos have a period specified by miniMaestroServoPeriod.
        /// The multiplied servos have a period specified by miniMaestroServoPeriod*servoMultiplier.
        /// 
        /// Valid values for this parameter are 1 to 256.
        /// </summary>
        public ushort ServoMultiplier { get; set; } = 1;

        /// <summary>
        /// Determines how serial bytes flow between the two USB COM ports, the TTL port,
        /// and the Maestro's serial command processor.
        /// </summary>
        public SerialMode SerialMode { get; set; } = SerialMode.SERIAL_MODE_UART_DETECT_BAUD_RATE;

        /// <summary>
        /// The fixed baud rate, in units of bits per second.  This gets stored in a
        /// different format on the usc.cs, so there will be rounding errors
        /// which get bigger at higher baud rates, but they will be less than
        /// 1% for baud rates of 120000 or less.
        /// 
        /// This parameter only applies if serial mode is USB UART Fixed Baud.
        /// 
        /// All values above 184 are valid, but values significantly higher than
        /// 250000 are subject to high rounding errors and the usc firmware might not
        /// be able to keep up with those higher data rates.  If the baud rate is too
        /// high and the firmware can't keep up, the Maestro will indicate this to you
        /// by generating a serial overrun or buffer full error.
        /// </summary>
        public uint FixedBaudRate { get; set; } = 9600;

        /// <summary>
        /// If true, then you must send a 7-bit CRC byte at the end of every serial
        /// command (except the Mini SSC II command).
        /// </summary>
        public bool EnableCRC { get; set; } = false;

        /// <summary>
        /// If true, then the Maestro will never go to sleep.  This lets you power 
        /// the processer off of USB even when the computer has gone to sleep and put
        /// all of its USB devices in the suspend state.
        /// </summary>
        public bool NeverSuspend { get; set; } = false;

        /// <summary>
        /// The serial device number used to identify this device in Pololu protocol
        /// commands.  Valid values are 0-127, default is 12.
        /// </summary>
        public byte SerialDeviceNumber { get; set; } = 12;

        /// <summary>
        /// The offset used to determine which Mini SSC commands this device will
        /// respond to.  The second byte of the Mini SSC command contains the servo
        /// number; the correspondence between servo number and maestro number (0-5)
        /// is servo# = miniSSCoffset + channel#.  Valid values are 0-254.
        /// </summary>
        public byte MiniSscOffset { get; set; } = 0;

        /// <summary>
        /// The time it takes for a serial timeout error to occur, in units of 10 ms.
        /// A value of 0 means no timeout error will occur.  All values 0-65535 are valid.
        /// </summary>
        public ushort SerialTimeout { get; set; } = 0;

        /// <summary>
        /// True if the script should not be started when the device starts up.
        /// False if the script should be started.
        /// </summary>
        public bool ScriptDone { get; set; } = true;

        /// <summary>
        /// A list of the configurable parameters for each channel, including
        /// name, type, home type, home position, range, neutral, min, max.
        /// </summary>
        public List<ChannelSetting> ChannelSettings { get; set; } = new List<ChannelSetting>();

        /// <summary>
        /// If true, this setting enables pullups for each channel 18-20 which
        /// is configured as an input.  This makes the input value be high by
        /// default, allowing the user to connect a button or switch without
        /// supplying their own pull-up resistor.  Thi setting only applies to
        /// the Mini Maestro 24-Channel Servo Controller.
        /// </summary>
        public bool EnablePullups { get; set; } = false;

        public IList<Sequence> Sequences { get; set; } = new List<Sequence>();

        /// <summary>
        /// The number of servos on the device.
        /// </summary>
        public byte ServoCount
        {
            get
            {
                return (byte)ChannelSettings.Count;
            }
        }

        /// <summary>
        /// true if when loading the script, the checksum did not match or there was an error in compilation, so that it had to be reset to an empty script
        /// </summary>
        public bool ScriptInconsistent { get; set; } = false;

        public string Script { get; private set; }

        public BytecodeProgram BytecodeProgram { get; private set; }

        

        public decimal PeriodInMicroseconds
        {
            get
            {
                if (ServoCount == 6)
                {
                    return UscClass.PeriodToMicroseconds(ServoPeriod, ServosAvailable);
                }
                else
                {
                    return MiniMaestroServoPeriod / 4;
                }
            }
        }

        public void SetAndCompileScript(string script)
        {
            Script = null;

            BytecodeProgram = BytecodeReader.Read(script, ServoCount != 6);

            // If no exceptions were raised, set the script.
            Script = script;
        }
    }
    */
}