using System;
using Pololu.Usc.Enums;
using Pololu.Usc.Models;
using System.Collections.Generic;
using System.Linq;
using Pololu.Usc.Exceptions;
using System.Threading;
using Pololu.Usc.Structs;

namespace Pololu.Usc
{
    public class MaestroController : IDisposable
    {
        /// <summary>
        /// Instructions are executed at 12 MHZ
        /// </summary>
        private const int INSTRUCTION_FREQUENCY = 12000000;
        private const byte SERVO6_STACK_SIZE = 32;
        private const byte SERVO6_CALL_STACK_SIZE = 10;
        private const uint SERVO6_SUB_OFFSET_BLOCKS = 64;
        private const ushort SERVO6_MAX_SCRIPT_LENGTH = 1024;
        private const byte OTHER_STACK_SIZE = 126;
        private const byte OTHER_CALL_STACK_SIZE = 126;
        private const uint OTHER_SUB_OFFSET_BLOCKS = 512;
        private const ushort OTHER_MAX_SCRIPT_LENGTH = 8192;
        private const byte SERVO_PARAMETERS_BYTES = 9;
        /// <summary>Pololu's USB vendor id.</summary>
        private static readonly ushort PololuVendorId = 0x1ffb;
        /// <summary>The Micro Maestro's product ID.</summary>
        /// <value>0x0089</value>
        private static readonly ushort[] ProductIDArray = { (ushort)MaestroType.Servo6, (ushort)MaestroType.Servo12, (ushort)MaestroType.Servo18, (ushort)MaestroType.Servo24 };

        private byte _firmwareVersionMinor = 0xFF;
        private byte _firmwareVersionMajor = 0xFF;
        private readonly UsbDevice _device;
        private bool _disposed;

        /// <summary>
        /// The device interface GUID used to detect the native USB interface
        /// of the Maestro Servo Controllers in windows.
        /// </summary>
        /// <remarks>From maestro.inf.</remarks>
        // public static Guid DeviceInterfaceGuid = new Guid("e0fbe39f-7670-4db6-9b1a-1dfb141014a7");
        /// <summary>
        /// An array of strings needed to detect which bootloaders are connected.
        /// when doing firmware upgrades.
        /// </summary>
        /*
        public static string[] BootloaderDeviceInstanceIdPrefixes
        {
            get
            {
                return new string[] { "USB\\VID_1FFB&PID_0088", "USB\\VID_1FFB&PID_008D", "USB\\VID_1FFB&PID_008E", "USB\\VID_1FFB&PID_008F" };
            }
        }
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


        */

        public static IEnumerable<DeviceDescription> GetDevices()
        {
            return UsbInterface.Instance.GetDeviceList(PololuVendorId, ProductIDArray);
        }

        public static MaestroController GetDevice(DeviceDescription description)
        {
            if (false == ProductIDArray.Contains(description.ProductId))
            {
                throw new InvalidProductIdentityException($"Unkown product id :{description.ProductId}");
            }
            return new MaestroController(description);
        }

        private MaestroController(DeviceDescription description)
        {
            _device = new UsbDevice(description);
        }

        public MaestroType Type => (MaestroType)_device.Description.ProductId;

        public byte MaxServoCount
        {
            get
            {
                switch (Type)
                {
                    case MaestroType.Servo6:
                        return 6;
                    case MaestroType.Servo12:
                        return 12;
                    case MaestroType.Servo18:
                        return 18;
                    case MaestroType.Servo24:
                        return 24;
                }
                throw new InvalidProductIdentityException($"Unkown product id :{_device.Description.ProductId}");
            }
        }

        public byte StackSize => Type == MaestroType.Servo6 ? SERVO6_STACK_SIZE : OTHER_STACK_SIZE;
        public byte CallStackSize => Type == MaestroType.Servo6 ? SERVO6_CALL_STACK_SIZE : OTHER_CALL_STACK_SIZE;
        private uint SubroutineOffsetBlocks => Type == MaestroType.Servo6 ? SERVO6_SUB_OFFSET_BLOCKS : OTHER_SUB_OFFSET_BLOCKS;
        public ushort MaxScriptLength => Type == MaestroType.Servo6 ? SERVO6_MAX_SCRIPT_LENGTH : OTHER_MAX_SCRIPT_LENGTH;

        public byte FirmwareVersionMinor
        {
            get
            {
                if (_firmwareVersionMinor == 0xFF)
                {
                    GetFirmwareVersion();
                }

                return _firmwareVersionMinor;
            }
        }

        public byte FirmwareVersionMajor
        {
            get
            {
                if (_firmwareVersionMajor == 0xFF)
                {
                    GetFirmwareVersion();
                }
                return _firmwareVersionMajor;
            }
        }

        public void Connect()
        {
            _device.Open();
        }


        public void StartBootloader()
        {
            _device.ControlTransfer(RequestType._0x40, Request.REQUEST_START_BOOTLOADER, 0, 0);
        }

        public void ReInitialize(int waitTimeMillis)
        {
            _device.ControlTransfer(RequestType._0x40, Request.REQUEST_REINITIALIZE, 0, 0);
            Thread.Sleep(waitTimeMillis);
            /*
            if (!MicroMaestro)
            {
                // Flush out any spurious performance flags that might have occurred.

                GetVariables(out MaestroVariables variables);
            }
            */
        }

        public void ClearErrors()
        {
            _device.ControlTransfer(RequestType._0x40, Request.REQUEST_CLEAR_ERRORS, 0, 0);
        }


        public void SetTarget(byte servo, ushort value)
        {
            _device.ControlTransfer(RequestType._0x40, Request.REQUEST_SET_TARGET, value, servo);
        }

        public void SetSpeed(byte servo, ushort value)
        {
            _device.ControlTransfer(RequestType._0x40, Request.REQUEST_SET_SERVO_VARIABLE, value, servo);
        }

        public void SetPWM(ushort dutyCycle, ushort period)
        {
            _device.ControlTransfer(RequestType._0x40, Request.REQUEST_SET_PWM, dutyCycle, period);
        }

        public void DisablePWM()
        {
            // TODO check it is valid. something wrong with this
            if (Type == MaestroType.Servo12)
            {
                SetTarget(8, 0);
            }
            else
            {
                SetTarget(12, 0);
            }
        }


        /// <summary>
        /// Returns the parameter number for the parameter of a given servo,
        /// given the corresponding parameter number for servo 0.
        /// </summary>
        /// <param name="p">e.g. PARAMETER_SERVO0_HOME</param>
        /// <param name="servo">Channel number.</param>
        Parameter SpecifyServo(Parameter p, byte servo)
        {
            return (Parameter)((byte)(p) + servo * SERVO_PARAMETERS_BYTES);
        }


        public void SetAcceleration(byte servo, ushort value)
        {
            // set the high bit of servo to specify acceleration
            _device.ControlTransfer(RequestType._0x40, Request.REQUEST_SET_SERVO_VARIABLE, value, (byte)(servo | 0x80));
        }

        public void GetVariables(out MaestroVariables variables, out short[] stack, out ushort[] callStack, out ServoStatus[] status)
        {
            if (Type == MaestroType.Servo6)
            {
                GetVariablesServo6(out variables, out stack, out callStack, out status);
            }
            else
            {
                GetVariablesOther(out variables);
                GetVariablesOther(out stack);
                GetVariablesOther(out callStack);
                GetVariablesOther(out status);
            }
        }

        public void GetVariables(out MaestroVariables variables)
        {
            if (Type == MaestroType.Servo6)
            {
                GetVariablesServo6(out variables, out short[] stack, out ushort[] callStack, out ServoStatus[] status);
            }
            else
            {
                GetVariablesOther(out variables);
            }
        }

        public void GetVariables(out ServoStatus[] servos)
        {
            if (Type == MaestroType.Servo6)
            {
                GetVariablesServo6(out MaestroVariables vars, out short[] stack, out ushort[] callStack, out servos);
            }
            else
            {
                GetVariablesOther(out servos);
            }
        }

        public void GetVariables(out short[] stack)
        {
            if (Type == MaestroType.Servo6)
            {
                GetVariablesServo6(out MaestroVariables variables, out stack, out ushort[] callStack, out ServoStatus[] servos);
            }
            else
            {
                GetVariablesOther(out stack);
            }
        }

        public void GetVariables(out ushort[] callStack)
        {
            if (Type == MaestroType.Servo6)
            {
                GetVariablesServo6(out MaestroVariables variables, out short[] stack, out callStack, out ServoStatus[] servos);
            }
            else
            {
                GetVariablesOther(out callStack);
            }
        }


        private unsafe void GetVariablesOther(out MaestroVariables variables)
        {
            // Get miscellaneous variables.
            MiniMaestroVariables tmp;
            var bytesRead = (uint)_device.ControlTransfer(RequestType._0xC0, Request.REQUEST_GET_VARIABLES, 0, 0, &tmp, (ushort)sizeof(MiniMaestroVariables));
            if (bytesRead != sizeof(MiniMaestroVariables))
            {
                throw new DataMisalignedException("Short read: " + bytesRead + " < " + sizeof(MiniMaestroVariables) + ".");
            }

            // Copy the variable data
            variables.StackPointer = tmp.StackPointer;
            variables.CallStackPointer = tmp.CallStackPointer;
            variables.Errors = tmp.Errors;
            variables.ProgramCounter = tmp.ProgramCounter;
            variables.ScriptDone = (byte)tmp.ScriptDone;
            variables.PerformanceFlags = tmp.PerformanceFlags;
        }

        private unsafe void GetVariablesOther(out short[] stack)
        {
            stack = new short[OTHER_STACK_SIZE];
            fixed (short* pointer = stack)
            {
                var bytesRead = (uint)_device.ControlTransfer(RequestType._0xC0, Request.REQUEST_GET_STACK, 0, 0, pointer, (ushort)(sizeof(short) * stack.Length));
                Array.Resize(ref stack, (int)(bytesRead / sizeof(short)));
            }
        }

        private unsafe void GetVariablesOther(out ushort[] callStack)
        {
            callStack = new ushort[OTHER_CALL_STACK_SIZE];
            fixed (ushort* pointer = callStack)
            {
                var bytesRead = (uint)_device.ControlTransfer(RequestType._0xC0, Request.REQUEST_GET_CALL_STACK, 0, 0, pointer, (ushort)(sizeof(ushort) * callStack.Length));
                Array.Resize(ref callStack, (int)(bytesRead / sizeof(ushort)));
            }
        }

        private unsafe void GetVariablesOther(out ServoStatus[] servos)
        {
            byte[] servoSettingsArray = new byte[MaxServoCount * sizeof(ServoStatus)];
            var bytesRead = (uint)_device.ControlTransfer(RequestType._0xC0, Request.REQUEST_GET_SERVO_SETTINGS, 0, 0, servoSettingsArray);
            if (bytesRead != servoSettingsArray.Length)
            {
                throw new DataMisalignedException("Short read: " + bytesRead + " < " + servoSettingsArray.Length + ".");
            }

            // Put the data in to a managed array object.
            servos = new ServoStatus[MaxServoCount];
            fixed (byte* pointer = servoSettingsArray)
            {
                for (byte i = 0; i < MaxServoCount; i++)
                {
                    servos[i] = *(ServoStatus*)(pointer + sizeof(ServoStatus) * i);
                }
            }
        }

        private unsafe void GetVariablesServo6(out MaestroVariables variables, out short[] stack, out ushort[] callStack, out ServoStatus[] servos)
        {
            byte[] array = new byte[sizeof(MicroMaestroVariables) + MaxServoCount * sizeof(ServoStatus)];

            _device.ControlTransfer(RequestType._0xC0, Request.REQUEST_GET_VARIABLES, 0, 0, array);

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

                servos = new ServoStatus[MaxServoCount];
                for (byte i = 0; i < MaxServoCount; i++)
                {
                    servos[i] = *(ServoStatus*)(pointer + sizeof(MicroMaestroVariables) + sizeof(ServoStatus) * i);
                }

                stack = new short[variables.StackPointer];
                for (byte i = 0; i < stack.Length; i++) { stack[i] = *(tmp.Stack + i); }

                callStack = new ushort[variables.CallStackPointer];
                for (byte i = 0; i < callStack.Length; i++) { callStack[i] = *(tmp.CallStack + i); }
            }
        }

        public void RestoreDefaultConfiguration()
        {
            SetRawParameterNoChecks((byte)Parameter.PARAMETER_INITIALIZED, 0xFF, 1);
            ReInitialize(1500);
        }

        public ParameterRange GetParameterRange(Parameter parameterId)
        {
            if (parameterId == Parameter.PARAMETER_INITIALIZED)
                return ParameterRange.u8;

            switch (parameterId)
            {
                case Parameter.PARAMETER_SERVOS_AVAILABLE:
                    return ParameterRange.u8;
                case Parameter.PARAMETER_SERVO_PERIOD:
                    return ParameterRange.u8;
                case Parameter.PARAMETER_MINI_MAESTRO_SERVO_PERIOD_L:
                    return ParameterRange.u8;
                case Parameter.PARAMETER_MINI_MAESTRO_SERVO_PERIOD_HU:
                    return ParameterRange.u16;
                case Parameter.PARAMETER_SERVO_MULTIPLIER:
                    return ParameterRange.u8;
                case Parameter.PARAMETER_CHANNEL_MODES_0_3:
                case Parameter.PARAMETER_CHANNEL_MODES_4_7:
                case Parameter.PARAMETER_CHANNEL_MODES_8_11:
                case Parameter.PARAMETER_CHANNEL_MODES_12_15:
                case Parameter.PARAMETER_CHANNEL_MODES_16_19:
                case Parameter.PARAMETER_CHANNEL_MODES_20_23:
                case Parameter.PARAMETER_ENABLE_PULLUPS:
                    return ParameterRange.u8;
                case Parameter.PARAMETER_SERIAL_MODE:
                    return new ParameterRange(1, 0, 3);
                case Parameter.PARAMETER_SERIAL_BAUD_DETECT_TYPE:
                    return new ParameterRange(1, 0, 1);
                case Parameter.PARAMETER_SERIAL_NEVER_SUSPEND:
                    return ParameterRange.boolean;
                case Parameter.PARAMETER_SERIAL_TIMEOUT:
                    return ParameterRange.u16;
                case Parameter.PARAMETER_SERIAL_ENABLE_CRC:
                    return ParameterRange.boolean;
                case Parameter.PARAMETER_SERIAL_DEVICE_NUMBER:
                    return ParameterRange.u7;
                case Parameter.PARAMETER_SERIAL_FIXED_BAUD_RATE:
                    return ParameterRange.u16;
                case Parameter.PARAMETER_SERIAL_MINI_SSC_OFFSET:
                    return new ParameterRange(1, 0, 254);
                case Parameter.PARAMETER_SCRIPT_CRC:
                    return ParameterRange.u16;
                case Parameter.PARAMETER_SCRIPT_DONE:
                    return ParameterRange.boolean;
            }

            // must be one of the servo parameters
            switch ((((byte)parameterId - (byte)Parameter.PARAMETER_SERVO0_HOME) % 9) +
                    (byte)Parameter.PARAMETER_SERVO0_HOME)
            {
                case (byte)Parameter.PARAMETER_SERVO0_HOME:
                case (byte)Parameter.PARAMETER_SERVO0_NEUTRAL:
                    return new ParameterRange(2, 0, 32440); // 32640 - 200
                case (byte)Parameter.PARAMETER_SERVO0_RANGE:
                    return new ParameterRange(1, 1, 50); // the upper limit could be adjusted
                case (byte)Parameter.PARAMETER_SERVO0_SPEED:
                case (byte)Parameter.PARAMETER_SERVO0_MAX:
                case (byte)Parameter.PARAMETER_SERVO0_MIN:
                case (byte)Parameter.PARAMETER_SERVO0_ACCELERATION:
                    return ParameterRange.u8;
            }

            throw new ArgumentException("Invalid parameterId " + parameterId.ToString() + ", can not determine the range of this parameter.");
        }

        public unsafe ushort GetRawParameter(Parameter parameter)
        {
            var range = GetParameterRange(parameter);
            ushort value = 0;
            byte[] array = new byte[range.Bytes];
                _device.ControlTransfer(RequestType._0xC0, Request.REQUEST_GET_PARAMETER, 0, (ushort)parameter, array);
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

        public void SetRawParameter(Parameter parameter, ushort value)
        {
            var range = GetParameterRange(parameter);
            range.ThrowIfNotValid(value, parameter.ToString());
            var bytes = range.Bytes;
            SetRawParameterNoChecks((ushort)parameter, value, bytes);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("Controller#" + _device.Description.Name);
            }
            _device.Dispose();
            _disposed = true;
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
            _device.ControlTransfer(RequestType._0x40, Request.REQUEST_SET_PARAMETER, value, index);
        }


        private void GetFirmwareVersion()
        {
            byte[] buffer = new byte[14];
            _device.ControlTransfer(RequestType._0x80, Request.REQUEST_GET_FIRMWARE_VERSION, 0x0100, 0x0000, buffer);
            _firmwareVersionMinor = (byte)((buffer[12] & 0xF) + (buffer[12] >> 4 & 0xF) * 10);
            _firmwareVersionMajor = (byte)((buffer[13] & 0xF) + (buffer[13] >> 4 & 0xF) * 10);
        }



        /*
         *  /// <summary>
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

         */
    }
}
