using System;

namespace Pololu.Usc
{
    internal static class MaestroHelpers
    {
        public static ushort ExponentialSpeedToNormalSpeed(byte exponentialSpeed)
        {
            // Maximum value of normalSpeed is 31*(1<<7)=3968

            int mantissa = exponentialSpeed >> 3;
            int exponent = exponentialSpeed & 7;

            return (ushort)(mantissa * (1 << exponent));
        }

        public static byte NormalSpeedToExponentialSpeed(ushort normalSpeed)
        {
            ushort mantissa = normalSpeed;
            byte exponent = 0;

            while (true)
            {
                if (mantissa < 32)
                {
                    // We have reached the correct representation.
                    return (byte)(exponent + (mantissa << 3));
                }

                if (exponent == 7)
                {
                    // The number is too big to express in this format.
                    return 0xFF;
                }

                // Try representing the number with a bigger exponent.
                exponent += 1;
                mantissa >>= 1;
            }
        }

        public static decimal PositionToMicroseconds(ushort position)
        {
            return position / 4M;
        }

        
        public static ushort MicrosecondsToPosition(decimal us)
        {
            return (ushort)(us * 4M);
        }

        /// <summary>
        /// The approximate number of microseconds represented by the servo
        /// period when PARAMETER_SERVO_PERIOD is set to this value.
        /// </summary>
        public static decimal PeriodToMicroseconds(ushort period, byte servosAvailiable)
        {
            return period * 256M * servosAvailiable / 12M;
        }

        /// <summary>
        /// The closest value of PARAMETER_SERVO_PERIOD for a given number of us per period.
        /// </summary>
        /// <returns>Amount of time allocated to each servo, in units of 256/12.</returns>
        public static byte MicrosecondsToPeriod(decimal us, byte servosAvailiable)
        {
            return (byte)Math.Round(us / 256M * 12M / servosAvailiable);
        }


        /// <summary>
        /// See Sec 16.3 of the PIC18F14K50 datasheet for information about SPBRG.
        /// On the umc01a, we have SYNC=0, BRG16=1, and BRGH=1, so the pure math
        /// formula for the baud rate is Baud = INSTRUCTION_FREQUENCY / (spbrg+1);
        /// </summary>
        public static uint ConvertSpbrgToBps(ushort spbrg, int instructionsFrequency)
        {
            if (spbrg == 0)
            {
                return 0;
            }

            return (uint)((instructionsFrequency + (spbrg + 1) / 2) / (spbrg + 1));
        }


        /// <summary>
        /// The converts from bps to SPBRG, so it is the opposite of convertSpbrgToBps.
        /// The purse math formula is spbrg = INSTRUCTION_FREQUENCY/Baud - 1.
        /// </summary>
        public static ushort ConvertBpsToSpbrg(uint bps, int instructionsFrequency)
        {
            if (bps == 0)
            {
                return 0;
            }

            return (ushort)((instructionsFrequency - bps / 2) / bps);
        }


        /// <summary>
        /// Converts channel number (0-5) to port mask bit number
        /// on the Micro Maestro.  Not useful on other Maestros.
        /// </summary>
        public static byte ChannelToPort(byte channel)
        {
            if (channel <= 3)
            {
                return channel;
            }
            else if (channel < 6)
            {
                return (byte)(channel + 2);
            }
            throw new ArgumentException("Invalid channel number " + channel);
        }
    }
}
