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
    /*
    public partial class UscClass : UsbDevice
    {
        /// <summary>
        /// The number of servos on the device.  This will be 6, 12, 18, or 24.
        /// </summary>
        public readonly byte ServoCount;

        ///<summary>The number of parameter bytes per servo.</summary>
        const byte ServoParameterBytes = 9;


        protected bool MicroMaestro
        {
            get
            {
                return ServoCount == 6;
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
            
            return settings;
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

        

        
    }
    */
}