using Pololu.Usc.Enums;
using Pololu.Usc.Models;
using Pololu.Usc.Structs;
using System;
using System.Collections.Generic;

namespace Pololu.Usc.Linux
{
    /// <summary>
    /// This class represents a Maestro that is connected to the computer.
    /// </summary>
    /// <remarks>
    /// Future improvements to this class might allow it to represent
    /// an abstract Maestro and attempt to re-connect whenever the connection is
    /// lost.
    /// </remarks>
    public partial class UscClass : UsbDevice
    {
        /// <summary>
        /// The device interface GUID used to detect the native USB interface
        /// of the Maestro Servo Controllers in windows.
        /// </summary>
        /// <remarks>From maestro.inf.</remarks>
        public static Guid DeviceInterfaceGuid = new Guid("e0fbe39f-7670-4db6-9b1a-1dfb141014a7");

        /// <summary>Pololu's USB vendor id.</summary>
        /// <value>0x1FFB</value>
        public const ushort VendorID = 0x1ffb;

        /// <summary>The Micro Maestro's product ID.</summary>
        /// <value>0x0089</value>
        public static readonly ushort[] ProductIDArray = new ushort[] { 0x0089, 0x008a, 0x008b, 0x008c };

        /// <value>
        /// Maestro USB servo controller
        /// </value>
        /// <remarks>
        /// Warning: EnglishName is used to choose the registry key.  So this should
        /// never be changed unless you change the code that selects the registry key.
        /// </remarks>
        public const string EnglishName = "Maestro USB servo controller";

        /// <summary>
        /// Instructions are executed at 12 MHZ
        /// </summary>
        const int INSTRUCTION_FREQUENCY = 12000000;

        /// <summary>
        /// An array of strings needed to detect which bootloaders are connected.
        /// when doing firmware upgrades.
        /// </summary>
        public static string[] BootloaderDeviceInstanceIdPrefixes
        {
            get
            {
                return new string[] { "USB\\VID_1FFB&PID_0088", "USB\\VID_1FFB&PID_008D", "USB\\VID_1FFB&PID_008E", "USB\\VID_1FFB&PID_008F" };
            }
        }

        /// <summary>
        /// Maestro
        /// </summary>
        public static string ShortProductName
        {
            get
            {
                return "Maestro";
            }
        }

        private static ushort ExponentialSpeedToNormalSpeed(byte exponentialSpeed)
        {
            // Maximum value of normalSpeed is 31*(1<<7)=3968

            int mantissa = exponentialSpeed >> 3;
            int exponent = exponentialSpeed & 7;

            return (ushort)(mantissa * (1 << exponent));
        }

        private static byte NormalSpeedToExponentialSpeed(ushort normalSpeed)
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
            return (decimal)position / 4M;
        }

        public static ushort MicrosecondsToPosition(decimal us)
        {
            return (ushort)(us * 4M);
        }

        /// <summary>
        /// The approximate number of microseconds represented by the servo
        /// period when PARAMETER_SERVO_PERIOD is set to this value.
        /// </summary>
        public static decimal PeriodToMicroseconds(ushort period, byte servos_available)
        {
            return (decimal)period * 256M * servos_available / 12M;
        }

        /// <summary>
        /// The closest value of PARAMETER_SERVO_PERIOD for a given number of us per period.
        /// </summary>
        /// <returns>Amount of time allocated to each servo, in units of 256/12.</returns>
        public static byte MicrosecondsToPeriod(decimal us, byte servos_avaiable)
        {
            return (byte)Math.Round(us / 256M * 12M / servos_avaiable);
        }

        /// <summary>
        /// See Sec 16.3 of the PIC18F14K50 datasheet for information about SPBRG.
        /// On the umc01a, we have SYNC=0, BRG16=1, and BRGH=1, so the pure math
        /// formula for the baud rate is Baud = INSTRUCTION_FREQUENCY / (spbrg+1);
        /// </summary>
        private static uint ConvertSpbrgToBps(ushort spbrg)
        {
            if (spbrg == 0)
            {
                return 0;
            }

            return (uint)((INSTRUCTION_FREQUENCY + (spbrg + 1) / 2) / (spbrg + 1));
        }

        /// <summary>
        /// The converts from bps to SPBRG, so it is the opposite of convertSpbrgToBps.
        /// The purse math formula is spbrg = INSTRUCTION_FREQUENCY/Baud - 1.
        /// </summary>
        private static ushort ConvertBpsToSpbrg(uint bps)
        {
            if (bps == 0)
            {
                return 0;
            }

            return (ushort)((INSTRUCTION_FREQUENCY - bps / 2) / bps);
        }

        /// <summary>
        /// Converts channel number (0-5) to port mask bit number
        /// on the Micro Maestro.  Not useful on other Maestros.
        /// </summary>
        private byte ChannelToPort(byte channel)
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

        /// <summary>
        /// The number of servos on the device.  This will be 6, 12, 18, or 24.
        /// </summary>
        public readonly byte ServoCount;

        ///<summary>The number of parameter bytes per servo.</summary>
        const byte ServoParameterBytes = 9;

        /// <summary>
        /// Returns the parameter number for the parameter of a given servo,
        /// given the corresponding parameter number for servo 0.
        /// </summary>
        /// <param name="p">e.g. PARAMETER_SERVO0_HOME</param>
        /// <param name="servo">Channel number.</param>
        Parameter SpecifyServo(Parameter p, byte servo)
        {
            return (Parameter)((byte)(p) + servo * ServoParameterBytes);
        }

        public UscClass(DeviceDescription deviceListItem) : base(deviceListItem)
        {
            // Determine the number of servos from the product id.
            switch (GetProductID())
            {
                case 0x89: ServoCount = 6; break;
                case 0x8A: ServoCount = 12; break;
                case 0x8B: ServoCount = 18; break;
                case 0x8C: ServoCount = 24; break;
                default: throw new Exception("Unknown product id " + GetProductID().ToString("x2") + ".");
            }
        }

        protected bool MicroMaestro
        {
            get
            {
                return ServoCount == 6;
            }
        }

        public byte StackSize
        {
            get
            {
                if (MicroMaestro)
                {
                    return MicroMaestroStackSize;
                }
                else
                {
                    return MiniMaestroStackSize;
                }
            }
        }

        public byte CallStackSize
        {
            get
            {
                if (MicroMaestro)
                {
                    return MicroMaestroCallStackSize;
                }
                else
                {
                    return MiniMaestroCallStackSize;
                }
            }
        }


        public static List<DeviceDescription> GetConnectedDevices()
        {
            return GetDeviceList(VendorID, ProductIDArray);
            /*
            try
            {
                return UsbDevice.getDeviceList(Usc.deviceInterfaceGuid);
            }
            catch (NotImplementedException)
            {
                // use vendor and product instead
                
            }
            */
        }

        Byte privateFirmwareVersionMajor = 0xFF;
        Byte privateFirmwareVersionMinor = 0xFF;

        public ushort FirmwareVersionMajor
        {
            get
            {
                if (privateFirmwareVersionMajor == 0xFF)
                {
                    GetFirmwareVersion();
                }
                return privateFirmwareVersionMajor;
            }
        }

        public byte FirmwareVersionMinor
        {
            get
            {
                if (privateFirmwareVersionMajor == 0xFF)
                {
                    GetFirmwareVersion();
                }
                return privateFirmwareVersionMinor;
            }
        }

        public string FirmwareVersionString
        {
            get
            {
                return FirmwareVersionMajor.ToString() + "." + FirmwareVersionMinor.ToString("D2");
            }
        }

        void GetFirmwareVersion()
        {
            byte[] buffer = new byte[14];

            try
            {
                ControlTransfer(0x80, 6, 0x0100, 0x0000, buffer);
            }
            catch (Exception exception)
            {
                throw new Exception("There was an error getting the firmware version from the device.", exception);
            }

            privateFirmwareVersionMinor = (byte)((buffer[12] & 0xF) + (buffer[12] >> 4 & 0xF) * 10);
            privateFirmwareVersionMajor = (byte)((buffer[13] & 0xF) + (buffer[13] >> 4 & 0xF) * 10);
        }

        /// <summary>
        /// Erases the entire script and subroutine address table from the devices.
        /// </summary>
        public void EraseScript()
        {
            try
            {
                ControlTransfer(0x40, (byte)Request.REQUEST_ERASE_SCRIPT, 0, 0);
            }
            catch (Exception e)
            {
                throw new Exception("There was an error erasing the script.", e);
            }
        }

        /// <summary>
        /// Stops and resets the script, sets the program counter to the beginning of the
        /// specified subroutine.  After this function has run, the script will be paused,
        /// so you must use setScriptDone() to start it.
        /// </summary>
        /// <param name="subroutine"></param>
        public void RestartScriptAtSubroutine(byte subroutine)
        {
            try
            {
                ControlTransfer(0x40, (byte)Request.REQUEST_RESTART_SCRIPT_AT_SUBROUTINE, 0, subroutine);
            }
            catch (Exception e)
            {
                throw new Exception("There was an error restarting the script at subroutine " + subroutine + ".", e);
            }
        }

        public void RestartScriptAtSubroutineWithParameter(byte subroutine, short parameter)
        {
            try
            {
                ControlTransfer(0x40, (byte)Request.REQUEST_RESTART_SCRIPT_AT_SUBROUTINE_WITH_PARAMETER, (ushort)parameter, subroutine);
            }
            catch (Exception e)
            {
                throw new Exception("There was an error restarting the script with a parameter at subroutine " + subroutine + ".", e);
            }
        }

        public void RestartScript()
        {
            try
            {
                ControlTransfer(0x40, (byte)Request.REQUEST_RESTART_SCRIPT, 0, 0);
            }
            catch (Exception e)
            {
                throw new Exception("There was an error restarting the script.", e);
            }
        }

        public void WriteScript(IList<byte> bytecode)
        {
            ushort block;
            for (block = 0; block < (bytecode.Count + 15) / 16; block++)
            {
                // write each block in a separate request
                byte[] blockBytes = new byte[16];

                ushort j;
                for (j = 0; j < 16; j++)
                {
                    if (block * 16 + j < bytecode.Count)
                        blockBytes[j] = bytecode[block * 16 + j];
                    else
                        blockBytes[j] = (byte)0xFF; // don't change flash if it is not necessary
                }

                try
                {
                    //                    System.Console.WriteLine((block)+": "+block_bytes[0]+" "+block_bytes[1]+" "+block_bytes[2]+" "+block_bytes[3]+" "+block_bytes[4]+" "+block_bytes[5]+" "+block_bytes[6]+" "+block_bytes[7]+" "+block_bytes[8]+" "+block_bytes[9]+" "+block_bytes[10]+" "+block_bytes[11]+" "+block_bytes[12]+" "+block_bytes[13]+" "+block_bytes[14]+" "+block_bytes[15]); // XXX
                    ControlTransfer(0x40, (byte)Request.REQUEST_WRITE_SCRIPT, 0, block,
                                           blockBytes);
                }
                catch (Exception e)
                {
                    throw new Exception("There was an error writing script block " + block + ".", e);
                }
            }
        }

        /// <remarks>
        /// Prior to 2011-7-20, this function had a bug in it that made
        /// subroutines 64-123 not work!
        /// </remarks>
        public void SetSubroutines(Dictionary<string, ushort> subroutineAddresses,
                                   Dictionary<string, byte> subroutineCommands)
        {
            byte[] subroutineData = new byte[256];

            ushort i;
            for (i = 0; i < 256; i++)
                subroutineData[i] = 0xFF; // initialize to the default flash state

            foreach (KeyValuePair<string, ushort> kvp in subroutineAddresses)
            {
                string name = kvp.Key;
                byte bytecode = subroutineCommands[name];

                if (bytecode == (byte)OperationCode.CALL)
                    continue; // skip CALLs - these do not get a position in the subroutine memory

                subroutineData[2 * (bytecode - 128)] = (byte)(kvp.Value % 256);
                subroutineData[2 * (bytecode - 128) + 1] = (byte)(kvp.Value >> 8);
            }

            ushort block;
            for (block = 0; block < 16; block++)
            {
                // write each block in a separate request
                byte[] blockBytes = new byte[16];

                ushort j;
                for (j = 0; j < 16; j++)
                {
                    blockBytes[j] = subroutineData[block * 16 + j];
                }

                try
                {
                    //                    System.Console.WriteLine((block + subroutineOffsetBlocks)+": "+block_bytes[0]+" "+block_bytes[1]+" "+block_bytes[2]+" "+block_bytes[3]+" "+block_bytes[4]+" "+block_bytes[5]+" "+block_bytes[6]+" "+block_bytes[7]+" "+block_bytes[8]+" "+block_bytes[9]+" "+block_bytes[10]+" "+block_bytes[11]+" "+block_bytes[12]+" "+block_bytes[13]+" "+block_bytes[14]+" "+block_bytes[15]); // XXX
                    ControlTransfer(0x40, (byte)Request.REQUEST_WRITE_SCRIPT, 0,
                                           (ushort)(block + SubroutineOffsetBlocks),
                                           blockBytes);
                }
                catch (Exception e)
                {
                    throw new Exception("There was an error writing subroutine block " + block + ".", e);
                }
            }
        }

        private uint SubroutineOffsetBlocks
        {
            get
            {
                switch (GetProductID())
                {
                    case 0x89: return 64;
                    case 0x8A: return 512;
                    case 0x8B: return 512;
                    case 0x8C: return 512;
                    default: throw new Exception("unknown product ID");
                }
            }
        }

        public void SetScriptDone(byte value)
        {
            try
            {
                ControlTransfer(0x40, (byte)Request.REQUEST_SET_SCRIPT_DONE, value, 0);
            }
            catch (Exception e)
            {
                throw new Exception("There was an error setting the script done.", e);
            }
        }

        public void StartBootloader()
        {
            try
            {
                ControlTransfer(0x40, (byte)Request.REQUEST_START_BOOTLOADER, 0, 0);
            }
            catch (Exception e)
            {
                throw new Exception("There was an error entering bootloader mode.", e);
            }
        }

        public void Reinitialize()
        {
            Reinitialize(50);
        }

        private void Reinitialize(int waitTime)
        {
            try
            {
                ControlTransfer(0x40, (byte)Request.REQUEST_REINITIALIZE, 0, 0);
            }
            catch (Exception e)
            {
                throw new Exception("There was an error re-initializing the device.", e);
            }

            System.Threading.Thread.Sleep(waitTime);
            if (!MicroMaestro)
            {
                // Flush out any spurious performance flags that might have occurred.

                GetVariables(out MaestroVariables variables);
            }
        }

        public void ClearErrors()
        {
            try
            {
                ControlTransfer(0x40, (byte)Request.REQUEST_CLEAR_ERRORS, 0, 0);
            }
            catch (Exception e)
            {
                throw new Exception("There was a USB communication error while clearing the servo errors.", e);
            }
        }

        /// <summary>
        /// Gets the complete set of status information for the Maestro.
        /// </summary>
        /// <remarks>If you are using a Mini Maestro and do not need all of
        /// the data provided by this function, you can save some CPU time
        /// by using the overloads with fewer arguments.</remarks>
        public unsafe void GetVariables(out MaestroVariables variables, out short[] stack, out ushort[] callStack, out ServoStatus[] servos)
        {
            if (MicroMaestro)
            {
                // On the Micro Maestro, this function requires just one control transfer:
                GetVariablesMicroMaestro(out variables, out stack, out callStack, out servos);
            }
            else
            {
                // On the Mini Maestro, this function requires four control transfers:
                GetVariablesMiniMaestro(out variables);
                GetVariablesMiniMaestro(out servos);
                GetVariablesMiniMaestro(out stack);
                GetVariablesMiniMaestro(out callStack);
            }
        }

        /// <summary>
        /// Gets a MaestroVariables struct representing the current status
        /// of the device.
        /// </summary>
        /// <remarks>If you are using the Micro Maestro and calling
        /// getVariables more than once in quick succession,
        /// then you can save some CPU time by just using the
        /// overload that has 4 arguments.
        /// </remarks>
        public void GetVariables(out MaestroVariables variables)
        {
            if (MicroMaestro)
            {
                GetVariablesMicroMaestro(out variables, out short[] stack, out ushort[] callStack, out ServoStatus[] servos);
            }
            else
            {
                GetVariablesMiniMaestro(out variables);
            }
        }

        /// <summary>
        /// Gets an array of ServoStatus structs representing
        /// the current status of all the channels.
        /// </summary>
        /// <remarks>If you are using the Micro Maestro and calling
        /// getVariables more than once in quick succession,
        /// then you can save some CPU time by just using the
        /// overload that has 4 arguments.
        /// </remarks>
        public void GetVariables(out ServoStatus[] servos)
        {
            if (MicroMaestro)
            {
                GetVariablesMicroMaestro(out MaestroVariables variables, out short[] stack, out ushort[] callStack, out servos);
            }
            else
            {
                GetVariablesMiniMaestro(out servos);
            }
        }

        /// <summary>
        /// Gets an array of shorts[] representing the current stack.
        /// The maximum size of the array is stackSize.
        /// </summary>
        /// <remarks>If you are using the Micro Maestro and calling
        /// getVariables more than once in quick succession,
        /// then you can save some CPU time by just using the
        /// overload that has 4 arguments.
        /// </remarks>
        public void GetVariables(out short[] stack)
        {
            if (MicroMaestro)
            {
                GetVariablesMicroMaestro(out MaestroVariables variables, out stack, out ushort[] callStack, out ServoStatus[] servos);
            }
            else
            {
                GetVariablesMiniMaestro(out stack);
            }
        }

        /// <summary>
        /// Gets an array of ushorts[] representing the current stack.
        /// The maximum size of the array is callStackSize.
        /// </summary>
        /// <remarks>If you are using the Micro Maestro and calling
        /// getVariables more than once in quick succession,
        /// then you can save some CPU time by just using the
        /// overload that has 4 arguments.
        /// </remarks>
        public void GetVariables(out ushort[] callStack)
        {
            if (MicroMaestro)
            {
                GetVariablesMicroMaestro(out MaestroVariables variables, out short[] stack, out callStack, out ServoStatus[] servos);
            }
            else
            {
                GetVariablesMiniMaestro(out callStack);
            }
        }

        public const int MicroMaestroStackSize = 32;
        public const int MicroMaestroCallStackSize = 10;

        public const int MiniMaestroStackSize = 126;
        public const int MiniMaestroCallStackSize = 126;

        private unsafe void GetVariablesMicroMaestro(out MaestroVariables variables, out short[] stack, out ushort[] callStack, out ServoStatus[] servos)
        {
            byte[] array = new byte[sizeof(MicroMaestroVariables) + ServoCount * sizeof(ServoStatus)];

            try
            {
                ControlTransfer(0xC0, (byte)Request.REQUEST_GET_VARIABLES, 0, 0, array);
            }
            catch (Exception e)
            {
                throw new Exception("There was an error getting the device variables.", e);
            }

            fixed (byte* pointer = array)
            {
                // copy the variable data
                MicroMaestroVariables tmp = *(MicroMaestroVariables*)pointer;
                variables.StackPointer = tmp.StackPointer;
                variables.CallStackPointer = tmp.CallStackPointer;
                variables.Errors = tmp.Errors;
                variables.ProgramCounter = tmp.ProgramCounter;
                variables.ScriptDone = tmp.ScriptDone;
                variables.PerformanceFlags = 0;

                servos = new ServoStatus[ServoCount];
                for (byte i = 0; i < ServoCount; i++)
                {
                    servos[i] = *(ServoStatus*)(pointer + sizeof(MicroMaestroVariables) + sizeof(ServoStatus) * i);
                }

                stack = new short[variables.StackPointer];
                for (byte i = 0; i < stack.Length; i++) { stack[i] = *(tmp.Stack + i); }

                callStack = new ushort[variables.CallStackPointer];
                for (byte i = 0; i < callStack.Length; i++) { callStack[i] = *(tmp.CallStack + i); }
            }
        }

        private unsafe void GetVariablesMiniMaestro(out MaestroVariables variables)
        {
            try
            {
                // Get miscellaneous variables.
                MiniMaestroVariables tmp;
                UInt32 bytesRead = ControlTransfer(0xC0, (byte)Request.REQUEST_GET_VARIABLES, 0, 0, &tmp, (ushort)sizeof(MiniMaestroVariables));
                if (bytesRead != sizeof(MiniMaestroVariables))
                {
                    throw new Exception("Short read: " + bytesRead + " < " + sizeof(MiniMaestroVariables) + ".");
                }

                // Copy the variable data
                variables.StackPointer = tmp.StackPointer;
                variables.CallStackPointer = tmp.CallStackPointer;
                variables.Errors = tmp.Errors;
                variables.ProgramCounter = tmp.ProgramCounter;
                variables.ScriptDone = (byte)tmp.ScriptDone;
                variables.PerformanceFlags = tmp.PerformanceFlags;
            }
            catch (Exception e)
            {
                throw new Exception("Error getting variables from device.", e);
            }
        }

        private unsafe void GetVariablesMiniMaestro(out ServoStatus[] servos)
        {
            try
            {
                byte[] servoSettingsArray = new byte[ServoCount * sizeof(ServoStatus)];

                // Get the raw data from the device.
                UInt32 bytesRead = ControlTransfer(0xC0, (byte)Request.REQUEST_GET_SERVO_SETTINGS, 0, 0, servoSettingsArray);
                if (bytesRead != servoSettingsArray.Length)
                {
                    throw new Exception("Short read: " + bytesRead + " < " + servoSettingsArray.Length + ".");
                }

                // Put the data in to a managed array object.
                servos = new ServoStatus[ServoCount];
                fixed (byte* pointer = servoSettingsArray)
                {
                    for (byte i = 0; i < ServoCount; i++)
                    {
                        servos[i] = *(ServoStatus*)(pointer + sizeof(ServoStatus) * i);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error getting channel settings from device.", e);
            }
        }

        private unsafe void GetVariablesMiniMaestro(out short[] stack)
        {
            try
            {
                // Get the data stack.
                stack = new short[MiniMaestroStackSize];
                fixed (short* pointer = stack)
                {
                    UInt32 bytesRead = ControlTransfer(0xC0, (byte)Request.REQUEST_GET_STACK, 0, 0, pointer, (ushort)(sizeof(short) * stack.Length));
                    Array.Resize<short>(ref stack, (int)(bytesRead / sizeof(short)));
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error getting stack from device.", e);
            }
        }

        private unsafe void GetVariablesMiniMaestro(out ushort[] callStack)
        {
            try
            {
                callStack = new ushort[MiniMaestroCallStackSize];
                fixed (ushort* pointer = callStack)
                {
                    UInt32 bytesRead = ControlTransfer(0xC0, (byte)Request.REQUEST_GET_CALL_STACK, 0, 0, pointer, (ushort)(sizeof(ushort) * callStack.Length));
                    Array.Resize<ushort>(ref callStack, (int)(bytesRead / sizeof(ushort)));
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error getting call stack from device.", e);
            }
        }

        public void SetTarget(byte servo, ushort value)
        {
            try
            {
                ControlTransfer(0x40, (byte)Request.REQUEST_SET_TARGET, value, servo);
            }
            catch (Exception e)
            {
                throw new Exception("Failed to set target of servo " + servo + " to " + value + ".", e);
            }
        }

        public void SetSpeed(byte servo, ushort value)
        {
            try
            {
                ControlTransfer(0x40, (byte)Request.REQUEST_SET_SERVO_VARIABLE, value, servo);
            }
            catch (Exception e)
            {
                throw new Exception("Failed to set speed of servo " + servo + " to " + value + ".", e);
            }
        }

        public void SetAcceleration(byte servo, ushort value)
        {
            // set the high bit of servo to specify acceleration
            try
            {
                ControlTransfer(0x40, (byte)Request.REQUEST_SET_SERVO_VARIABLE,
                                       value, (byte)(servo | 0x80));
            }
            catch (Exception e)
            {
                throw new Exception("Failed to set acceleration of servo " + servo + " to " + value + ".", e);
            }
        }

        public void SetUscSettings(UscSettings settings, bool newScript)
        {
            SetRawParameter(Parameter.PARAMETER_SERIAL_MODE, (byte)settings.SerialMode);
            SetRawParameter(Parameter.PARAMETER_SERIAL_FIXED_BAUD_RATE, ConvertBpsToSpbrg(settings.FixedBaudRate));
            SetRawParameter(Parameter.PARAMETER_SERIAL_ENABLE_CRC, (ushort)(settings.EnableCRC? 1 : 0));
            SetRawParameter(Parameter.PARAMETER_SERIAL_NEVER_SUSPEND, (ushort)(settings.NeverSuspend ? 1 : 0));
            SetRawParameter(Parameter.PARAMETER_SERIAL_DEVICE_NUMBER, settings.SerialDeviceNumber);
            SetRawParameter(Parameter.PARAMETER_SERIAL_MINI_SSC_OFFSET, settings.MiniSscOffset);
            SetRawParameter(Parameter.PARAMETER_SERIAL_TIMEOUT, settings.SerialTimeout);
            SetRawParameter(Parameter.PARAMETER_SCRIPT_DONE, (ushort)(settings.ScriptDone ? 1 : 0));

            if (ServoCount == 6)
            {
                SetRawParameter(Parameter.PARAMETER_SERVOS_AVAILABLE, settings.ServosAvailable);
                SetRawParameter(Parameter.PARAMETER_SERVO_PERIOD, settings.ServoPeriod);
            }
            else
            {
                SetRawParameter(Parameter.PARAMETER_MINI_MAESTRO_SERVO_PERIOD_L, (byte)(settings.MiniMaestroServoPeriod & 0xFF));
                SetRawParameter(Parameter.PARAMETER_MINI_MAESTRO_SERVO_PERIOD_HU, (ushort)(settings.MiniMaestroServoPeriod >> 8));

                byte multiplier;
                if (settings.ServoMultiplier < 1)
                {
                    multiplier = 0;
                }
                else if (settings.ServoMultiplier > 256)
                {
                    multiplier = 255;
                }
                else
                {
                    multiplier = (byte)(settings.ServoMultiplier - 1);
                }
                SetRawParameter(Parameter.PARAMETER_SERVO_MULTIPLIER, multiplier);
            }

            if (ServoCount > 18)
            {
                SetRawParameter(Parameter.PARAMETER_ENABLE_PULLUPS, (ushort)(settings.EnablePullups ? 1 : 0));
            }

            // RegistryKey key = openRegistryKey();

            byte ioMask = 0;
            byte outputMask = 0;
            byte[] channelModeBytes = new byte[6] { 0, 0, 0, 0, 0, 0 };

            for (byte i = 0; i < ServoCount; i++)
            {
                ChannelSetting setting = settings.ChannelSettings[i];

                // key.SetValue("servoName" + i.ToString("d2"), setting.name, RegistryValueKind.String);

                if (MicroMaestro)
                {
                    if (setting.Mode == ChannelMode.Input || setting.Mode == ChannelMode.Output)
                    {
                        ioMask |= (byte)(1 << ChannelToPort(i));
                    }

                    if (setting.Mode == ChannelMode.Output)
                    {
                        outputMask |= (byte)(1 << ChannelToPort(i));
                    }
                }
                else
                {
                    channelModeBytes[i >> 2] |= (byte)((byte)setting.Mode << ((i & 3) << 1));
                }

                // Make sure that HomeMode is "Ignore" for inputs.  This is also done in
                // fixUscSettings.
                HomeMode correctedHomeMode = setting.HomeMode;
                if (setting.Mode == ChannelMode.Input)
                {
                    correctedHomeMode = HomeMode.Ignore;
                }

                // Compute the raw value of the "home" parameter.
                ushort home;
                if (correctedHomeMode == HomeMode.Off) home = 0;
                else if (correctedHomeMode == HomeMode.Ignore) home = 1;
                else home = (ushort)setting.Mode;
                SetRawParameter(SpecifyServo(Parameter.PARAMETER_SERVO0_HOME, i), home);

                SetRawParameter(SpecifyServo(Parameter.PARAMETER_SERVO0_MIN, i), (ushort)(setting.Minimum / 64));
                SetRawParameter(SpecifyServo(Parameter.PARAMETER_SERVO0_MAX, i), (ushort)(setting.Maximum / 64));
                SetRawParameter(SpecifyServo(Parameter.PARAMETER_SERVO0_NEUTRAL, i), setting.Neutral);
                SetRawParameter(SpecifyServo(Parameter.PARAMETER_SERVO0_RANGE, i), (ushort)(setting.Range / 127));
                SetRawParameter(SpecifyServo(Parameter.PARAMETER_SERVO0_SPEED, i), NormalSpeedToExponentialSpeed(setting.Speed));
                SetRawParameter(SpecifyServo(Parameter.PARAMETER_SERVO0_ACCELERATION, i), setting.Acceleration);
            }

            if (MicroMaestro)
            {
                SetRawParameter(Parameter.PARAMETER_IO_MASK_C, ioMask);
                SetRawParameter(Parameter.PARAMETER_OUTPUT_MASK_C, outputMask);
            }
            else
            {
                for (byte i = 0; i < 6; i++)
                {
                    SetRawParameter(Parameter.PARAMETER_CHANNEL_MODES_0_3 + i, channelModeBytes[i]);
                }
            }

            if (newScript)
            {
                SetScriptDone(1); // stop the script

                // load the new script
                BytecodeProgram program = settings.BytecodeProgram;
                IList<byte> byteList = program.GetByteList();
                if (byteList.Count > MaxScriptLength)
                {
                    throw new Exception("Script too long for device (" + byteList.Count + " bytes)");
                }
                if (byteList.Count < MaxScriptLength)
                {
                    // if possible, add QUIT to the end to prevent mysterious problems with
                    // unterminated scripts
                    byteList.Add((byte)OperationCode.QUIT);
                }
                EraseScript();
                SetSubroutines(program.subroutineAddresses, program.subroutineCommands);
                WriteScript(byteList);
                SetRawParameter(Parameter.PARAMETER_SCRIPT_CRC, program.GetCRC());

                // Save the script in the registry
                // key.SetValue("script", settings.script, RegistryValueKind.String);
            }

            // Sequence.saveSequencesInRegistry(settings.sequences, key);

            // key.Close(); // This might be needed to flush the changes.
        }

        /// <summary>
        /// Tries to open the registry key that holds the information for this device.
        /// If the key does not exist, creates it.  Returns the key.
        /// </summary>
        /// <returns></returns>
        /* 
        private RegistryKey openRegistryKey()
        {
            string keyname = "Software\\Pololu\\" + englishName + "\\" + getSerialNumber();
            RegistryKey key = Registry.CurrentUser.OpenSubKey(keyname, true);
            if (key == null)
            {
                key = Registry.CurrentUser.CreateSubKey(keyname);
            }
            return key;
        }
        */

        private void SetRawParameter(Parameter parameter, ushort value)
        {
            Range range = UscClass.GetRange(parameter);
            RequireArgumentRange(value, range.MinimumValue, range.MaximumValue, parameter.ToString());
            int bytes = range.Bytes;
            SetRawParameterNoChecks((ushort)parameter, value, bytes);
        }

        /// <summary>
        /// Sets the parameter without checking the range or bytes
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="value"></param>
        /// <param name="bytes"></param>
        private void SetRawParameterNoChecks(ushort parameter, ushort value, int bytes)
        {
            ushort index = (ushort)((bytes << 8) + parameter); // high bytes = # of bytes
            try
            {
                ControlTransfer(0x40, (byte)Request.REQUEST_SET_PARAMETER, value, index);
            }
            catch (Exception e)
            {
                throw new Exception("There was an error setting parameter " + parameter.ToString() + " on the device.", e);
            }
        }

        private unsafe ushort GetRawParameter(Parameter parameter)
        {
            Range range = UscClass.GetRange(parameter);
            ushort value = 0;
            byte[] array = new byte[range.Bytes];
            try
            {
                ControlTransfer(0xC0, (byte)Request.REQUEST_GET_PARAMETER, 0, (ushort)parameter, array);
            }
            catch (Exception e)
            {
                throw new Exception("There was an error getting parameter " + parameter.ToString() + " from the device.", e);
            }
            if (range.Bytes == 1)
            {
                // read a single byte
                fixed (byte* pointer = array)
                {
                    value = *(byte*)pointer;
                }
            }
            else
            {
                // read two bytes
                fixed (byte* pointer = array)
                {
                    value = *(ushort*)pointer;
                }
            }
            return value;
        }

        /// <summary>
        /// Gets a settings object, pulling some info from the registry and some from the device.
        /// If there is an inconsistency, a special flag is set.
        /// </summary>
        /// <returns></returns>
        public UscSettings GetUscSettings()
        {
            var settings = new UscSettings
            {
                SerialMode = (SerialMode)GetRawParameter(Parameter.PARAMETER_SERIAL_MODE),
                FixedBaudRate = ConvertSpbrgToBps(GetRawParameter(Parameter.PARAMETER_SERIAL_FIXED_BAUD_RATE)),
                EnableCRC = GetRawParameter(Parameter.PARAMETER_SERIAL_ENABLE_CRC) != 0,
                NeverSuspend = GetRawParameter(Parameter.PARAMETER_SERIAL_NEVER_SUSPEND) != 0,
                SerialDeviceNumber = (byte)GetRawParameter(Parameter.PARAMETER_SERIAL_DEVICE_NUMBER),
                MiniSscOffset = (byte)GetRawParameter(Parameter.PARAMETER_SERIAL_MINI_SSC_OFFSET),
                SerialTimeout = GetRawParameter(Parameter.PARAMETER_SERIAL_TIMEOUT),
                ScriptDone = GetRawParameter(Parameter.PARAMETER_SCRIPT_DONE) != 0
            };

            if (ServoCount == 6)
            {
                settings.ServosAvailable = (byte)GetRawParameter(Parameter.PARAMETER_SERVOS_AVAILABLE);
                settings.ServoPeriod = (byte)GetRawParameter(Parameter.PARAMETER_SERVO_PERIOD);
            }
            else
            {
                UInt32 tmp = (UInt32)(GetRawParameter(Parameter.PARAMETER_MINI_MAESTRO_SERVO_PERIOD_HU) << 8);
                tmp |= (byte)GetRawParameter(Parameter.PARAMETER_MINI_MAESTRO_SERVO_PERIOD_L);
                settings.MiniMaestroServoPeriod = tmp;

                settings.ServoMultiplier = (ushort)(GetRawParameter(Parameter.PARAMETER_SERVO_MULTIPLIER) + 1);
            }

            if (ServoCount > 18)
            {
                settings.EnablePullups = GetRawParameter(Parameter.PARAMETER_ENABLE_PULLUPS) != 0;
            }

            byte ioMask = 0;
            byte outputMask = 0;
            byte[] channelModeBytes = new Byte[6];

            if (MicroMaestro)
            {
                ioMask = (byte)GetRawParameter(Parameter.PARAMETER_IO_MASK_C);
                outputMask = (byte)GetRawParameter(Parameter.PARAMETER_OUTPUT_MASK_C);
            }
            else
            {
                for (byte i = 0; i < 6; i++)
                {
                    channelModeBytes[i] = (byte)GetRawParameter(Parameter.PARAMETER_CHANNEL_MODES_0_3 + i);
                }
            }

            for (byte i = 0; i < ServoCount; i++)
            {
                // Initialize the ChannelSettings objects and 
                // set all parameters except name and mode.
                ChannelSetting setting = new ChannelSetting();

                if (MicroMaestro)
                {
                    byte bitmask = (byte)(1 << ChannelToPort(i));
                    if ((ioMask & bitmask) == 0)
                    {
                        setting.Mode = ChannelMode.Servo;
                    }
                    else if ((outputMask & bitmask) == 0)
                    {
                        setting.Mode = ChannelMode.Input;
                    }
                    else
                    {
                        setting.Mode = ChannelMode.Output;
                    }
                }
                else
                {
                    setting.Mode = (ChannelMode)((channelModeBytes[i >> 2] >> ((i & 3) << 1)) & 3);
                }

                ushort home = GetRawParameter(SpecifyServo(Parameter.PARAMETER_SERVO0_HOME, i));
                if (home == 0)
                {
                    setting.HomeMode = HomeMode.Off;
                    setting.Home = 0;
                }
                else if (home == 1)
                {
                    setting.HomeMode = HomeMode.Ignore;
                    setting.Home = 0;
                }
                else
                {
                    setting.HomeMode = HomeMode.Goto;
                    setting.Home = home;
                }

                setting.Minimum = (ushort)(64 * GetRawParameter(SpecifyServo(Parameter.PARAMETER_SERVO0_MIN, i)));
                setting.Maximum = (ushort)(64 * GetRawParameter(SpecifyServo(Parameter.PARAMETER_SERVO0_MAX, i)));
                setting.Neutral = GetRawParameter(SpecifyServo(Parameter.PARAMETER_SERVO0_NEUTRAL, i));
                setting.Range = (ushort)(127 * GetRawParameter(SpecifyServo(Parameter.PARAMETER_SERVO0_RANGE, i)));
                setting.Speed = ExponentialSpeedToNormalSpeed((byte)GetRawParameter(SpecifyServo(Parameter.PARAMETER_SERVO0_SPEED, i)));
                setting.Acceleration = (byte)GetRawParameter(SpecifyServo(Parameter.PARAMETER_SERVO0_ACCELERATION, i));

                settings.ChannelSettings.Add(setting);
            }

            // RegistryKey key = openRegistryKey();
            /*
            if (key != null)
            {
                // Get names for servos from the registry.
                for (byte i = 0; i < servoCount; i++)
                {
                    settings.channelSettings[i].name = (string)key.GetValue("servoName" + i.ToString("d2"), "");
                }

                // Get the script from the registry
                string script = (string)key.GetValue("script");
                if (script == null)
                    script = "";
                try
                {
                    // compile it to get the checksum
                    settings.setAndCompileScript(script);

                    BytecodeProgram program = settings.bytecodeProgram;
                    if (program.getByteList().Count > maxScriptLength)
                    {
                        throw new Exception();
                    }
                    if (program.getCRC() != (ushort)getRawParameter(uscParameter.PARAMETER_SCRIPT_CRC))
                    {
                        throw new Exception();
                    }
                }
                catch (Exception)
                {
                    // no script found or error compiling - leave script at ""
                    settings.scriptInconsistent = true;
                }

                // Get the sequences from the registry.
                settings.sequences = Sequencer.Sequence.readSequencesFromRegistry(key, servoCount);
            }
            */
            return settings;
        }

        public ushort MaxScriptLength
        {
            get
            {
                if (MicroMaestro)
                {
                    return 1024;
                }
                else
                {
                    return 8192;
                }
            }
        }

        private static void RequireArgumentRange(uint argumentValue, Int32 minimum, Int32 maximum, String argumentName)
        {
            if (argumentValue < minimum || argumentValue > maximum)
            {
                throw new ArgumentException("The " + argumentName + " must be between " + minimum +
                    " and " + maximum + " but the value given was " + argumentValue);
            }
        }

        public void RestoreDefaultConfiguration()
        {
            SetRawParameterNoChecks((byte)Parameter.PARAMETER_INITIALIZED, (ushort)0xFF, 1);
            Reinitialize(1500);
        }

        public void FixSettings(UscSettings settings, List<string> warnings)
        {
            // Discard extra channels if needed.
            if (settings.ServoCount > ServoCount)
            {
                warnings.Add("The settings loaded include settings for " + settings.ServoCount + " channels, but this device has only " + ServoCount + " channels.  The extra channel settings will be ignored.");
                settings.ChannelSettings.RemoveRange(ServoCount, settings.ServoCount - ServoCount);
                var lst = new List<string>();
            }

            // Add channels if needed.
            if (settings.ServoCount < ServoCount)
            {
                warnings.Add("The settings loaded include settings for only " + settings.ServoCount + " channels, but this device has " + ServoCount + " channels.  The other channels will be initialized with default settings.");
                while (settings.ServoCount < ServoCount)
                {
                    ChannelSetting cs = new ChannelSetting();
                    if (MicroMaestro && settings.ServosAvailable <= settings.ServoCount)
                    {
                        cs.Mode = ChannelMode.Input;
                    }
                    settings.ChannelSettings.Add(cs);
                }
            }

            // Prevent users from experiencing the bug with Ignore mode in Micro Maestro v1.00.
            if (settings.ServoCount == 6 && FirmwareVersionMajor <= 1 && FirmwareVersionMinor == 0)
            {
                bool servoIgnoreWarningShown = false;

                foreach (ChannelSetting setting in settings.ChannelSettings)
                {
                    if ((setting.Mode == ChannelMode.Servo) && setting.HomeMode == HomeMode.Ignore)
                    {
                        setting.HomeMode = HomeMode.Off;

                        if (!servoIgnoreWarningShown)
                        {
                            warnings.Add("Ignore mode does not work for servo channels on the Micro Maestro 6-Channel Servo Controller firmware versions prior to 1.01.\nYour channels will be changed to Off mode.\nVisit Pololu.com for a firmware upgrade.");
                            servoIgnoreWarningShown = true;
                        }
                    }
                }
            }

            // Set homeMode to ignore for inputs (silently, because it's not the user's fault).
            foreach (ChannelSetting cs in settings.ChannelSettings)
            {
                switch (cs.Mode)
                {
                    case ChannelMode.Input:
                        {
                            cs.HomeMode = HomeMode.Ignore;
                            cs.Minimum = 0;
                            cs.Maximum = 1024; // Should probably be 1023, but this is the tradition from the Micro Maestros.
                            cs.Speed = 0;
                            cs.Acceleration = 0;
                            cs.Neutral = 1024;
                            cs.Range = 1905;
                            break;
                        }
                    case ChannelMode.Output:
                        {
                            cs.Minimum = 3986;
                            cs.Maximum = 8000;
                            cs.Speed = 0;
                            cs.Acceleration = 0;
                            cs.Neutral = 6000;
                            cs.Range = 1905;
                            break;
                        }
                }
            }

            if (settings.SerialDeviceNumber >= 128)
            {
                settings.SerialDeviceNumber = 12;
                warnings.Add("The serial device number must be less than 128.  It will be changed to 12.");
            }
        }

        protected static Range GetRange(Parameter parameterId)
        {
            if (parameterId == Parameter.PARAMETER_INITIALIZED)
                return Range.u8;

            switch (parameterId)
            {
                case Parameter.PARAMETER_SERVOS_AVAILABLE:
                    return Range.u8;
                case Parameter.PARAMETER_SERVO_PERIOD:
                    return Range.u8;
                case Parameter.PARAMETER_MINI_MAESTRO_SERVO_PERIOD_L:
                    return Range.u8;
                case Parameter.PARAMETER_MINI_MAESTRO_SERVO_PERIOD_HU:
                    return Range.u16;
                case Parameter.PARAMETER_SERVO_MULTIPLIER:
                    return Range.u8;
                case Parameter.PARAMETER_CHANNEL_MODES_0_3:
                case Parameter.PARAMETER_CHANNEL_MODES_4_7:
                case Parameter.PARAMETER_CHANNEL_MODES_8_11:
                case Parameter.PARAMETER_CHANNEL_MODES_12_15:
                case Parameter.PARAMETER_CHANNEL_MODES_16_19:
                case Parameter.PARAMETER_CHANNEL_MODES_20_23:
                case Parameter.PARAMETER_ENABLE_PULLUPS:
                    return Range.u8;
                case Parameter.PARAMETER_SERIAL_MODE:
                    return new Range(1, 0, 3);
                case Parameter.PARAMETER_SERIAL_BAUD_DETECT_TYPE:
                    return new Range(1, 0, 1);
                case Parameter.PARAMETER_SERIAL_NEVER_SUSPEND:
                    return Range.boolean;
                case Parameter.PARAMETER_SERIAL_TIMEOUT:
                    return Range.u16;
                case Parameter.PARAMETER_SERIAL_ENABLE_CRC:
                    return Range.boolean;
                case Parameter.PARAMETER_SERIAL_DEVICE_NUMBER:
                    return Range.u7;
                case Parameter.PARAMETER_SERIAL_FIXED_BAUD_RATE:
                    return Range.u16;
                case Parameter.PARAMETER_SERIAL_MINI_SSC_OFFSET:
                    return new Range(1, 0, 254);
                case Parameter.PARAMETER_SCRIPT_CRC:
                    return Range.u16;
                case Parameter.PARAMETER_SCRIPT_DONE:
                    return Range.boolean;
            }

            // must be one of the servo parameters
            switch ((((byte)parameterId - (byte)Parameter.PARAMETER_SERVO0_HOME) % 9) +
                    (byte)Parameter.PARAMETER_SERVO0_HOME)
            {
                case (byte)Parameter.PARAMETER_SERVO0_HOME:
                case (byte)Parameter.PARAMETER_SERVO0_NEUTRAL:
                    return new Range(2, 0, 32440); // 32640 - 200
                case (byte)Parameter.PARAMETER_SERVO0_RANGE:
                    return new Range(1, 1, 50); // the upper limit could be adjusted
                case (byte)Parameter.PARAMETER_SERVO0_SPEED:
                case (byte)Parameter.PARAMETER_SERVO0_MAX:
                case (byte)Parameter.PARAMETER_SERVO0_MIN:
                case (byte)Parameter.PARAMETER_SERVO0_ACCELERATION:
                    return Range.u8;
            }

            throw new ArgumentException("Invalid parameterId " + parameterId.ToString() + ", can not determine the range of this parameter.");
        }

        protected struct Range
        {
            public byte Bytes;
            public int MinimumValue;
            public int MaximumValue;

            internal Range(byte bytes, int minimumValue, int maximumValue)
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

            internal static Range u32 = new Range(4, 0, 0x7FFFFFFF);
            internal static Range u16 = new Range(2, 0, 0xFFFF);
            internal static Range u12 = new Range(2, 0, 0x0FFF);
            internal static Range u10 = new Range(2, 0, 0x03FF);
            internal static Range u8 = new Range(1, 0, 0xFF);
            internal static Range u7 = new Range(1, 0, 0x7F);
            internal static Range boolean = new Range(1, 0, 1);
        }

        public void SetPWM(ushort dutyCycle, ushort period)
        {
            ControlTransfer(0x40, (byte)Request.REQUEST_SET_PWM, dutyCycle, period);
        }

        public void DisablePWM()
        {
            if (GetProductID() == 0x008a)
                SetTarget(8, 0);
            else
                SetTarget(12, 0);
        }
    }
}