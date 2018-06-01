using Pololu.Usc.Enums;
using Pololu.Usc.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Pololu.Usc
{
    internal static class ConfigurationFile
    {
        /// <summary>
        /// Parses a saved configuration file and returns a UscSettings object.
        /// </summary>
        /// <param name="ref warnings">A list of ref warnings.  Whenever something goes
        /// wrong with the file loading, a warning will be added to this list.
        /// The ref warnings are not fatal; if the function returns it will return
        /// a valid UscSettings object.
        /// </param>
        /// <param name="sr">The file to read from.</param>
        /// <remarks>This function is messy.  Maybe I should have tried the XPath
        /// library.</remarks>
        public static UscSettings Load(StreamReader sr, IList<String> warnings)
        {
            XmlReader reader = XmlReader.Create(sr);

            UscSettings settings = new UscSettings();

            string script = "";

            // The x prefix means "came directly from XML"
            Dictionary<String, String> xParams = new Dictionary<string, string>();

            // Only read the data inside the UscSettings element.
            reader.ReadToFollowing("UscSettings");
            ReadAttributes(reader, xParams);
            reader = reader.ReadSubtree();

            // Check the version number
            if (!xParams.ContainsKey("Version"))
            {
                warnings.Add("This file has no version number, so it might have been read incorrectly.");
            }
            else if (xParams["Version"] != "1")
            {
                warnings.Add("Unrecognized settings file version \"" + xParams["Version"] + "\".");
            }

            reader.Read(); // this is needed, otherwise the first tag inside uscSettings doesn't work work (not sure why)

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "Channels")
                {
                    // We found the Channels tag.

                    // Read the ServosAvailable and ServoPeriod attributes from it in to our collection.
                    ReadAttributes(reader, xParams);

                    // Make a reader that can only read the stuff inside the Channels tag.
                    var channelsReader = reader.ReadSubtree();

                    // For each Channel tag...
                    while (channelsReader.ReadToFollowing("Channel"))
                    {
                        // Read all the attributes.
                        Dictionary<String, String> xChannel = new Dictionary<string, string>();
                        ReadAttributes(channelsReader, xChannel);

                        // Transform the attributes in to a ChannelSetting object.
                        ChannelSetting cs = new ChannelSetting();
                        if (AssertKey("Name", ref xChannel, ref warnings))
                        {
                            cs.Name = xChannel["Name"];
                        }

                        if (AssertKey("Mode", ref xChannel, ref warnings))
                        {
                            switch (xChannel["Mode"].ToLowerInvariant())
                            {
                                case "ServoMultiplied": cs.Mode = ChannelMode.ServoMultiplied; break;
                                case "Servo": cs.Mode = ChannelMode.Servo; break;
                                case "Input": cs.Mode = ChannelMode.Input; break;
                                case "Output": cs.Mode = ChannelMode.Output; break;
                                default: warnings.Add("Invalid mode \"" + xChannel["Mode"] + "\"."); break;
                            }
                        }

                        if (AssertKey("HomeMode", ref xChannel, ref warnings))
                        {
                            switch (xChannel["HomeMode"].ToLowerInvariant())
                            {
                                case "Goto": cs.HomeMode = HomeMode.Goto; break;
                                case "Off": cs.HomeMode = HomeMode.Off; break;
                                case "Ignore": cs.HomeMode = HomeMode.Ignore; break;
                                default: warnings.Add("Invalid homemode \"" + xChannel["HomeMode"] + "\"."); break;
                            }
                        }

                        if (AssertKey("Min", ref xChannel, ref warnings))
                        {
                            cs.Minimum = ParseU16(xChannel["Min"], "Min", ref warnings);
                        }
                        if (AssertKey("Max", ref xChannel, ref warnings))
                        {
                            cs.Maximum = ParseU16(xChannel["Max"], "Max", ref warnings);
                        }
                        if (AssertKey("Home", ref xChannel, ref warnings))
                        {
                            cs.Home = ParseU16(xChannel["Home"], "Home", ref warnings);
                        }
                        if (AssertKey("Speed", ref xChannel, ref warnings))
                        {
                            cs.Speed = ParseU16(xChannel["Speed"], "Speed", ref warnings);
                        }
                        if (AssertKey("Acceleration", ref xChannel, ref warnings))
                        {
                            cs.Acceleration = ParseU8(xChannel["Acceleration"], "Acceleration", ref warnings);
                        }
                        if (AssertKey("Neutral", ref xChannel, ref warnings))
                        {
                            cs.Neutral = ParseU16(xChannel["Neutral"], "Neutral", ref warnings);
                        }
                        if (AssertKey("Range", ref xChannel, ref warnings))
                        {
                            cs.Range = ParseU16(xChannel["Range"], "Range", ref warnings);
                        }

                        settings.ChannelSettings.Add(cs);
                    }

                    if (channelsReader.ReadToFollowing("Channel"))
                    {
                        warnings.Add("More than " + settings.ServoCount + " channel elements were found.  The extra elements have been discarded.");
                    }

                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "Sequences")
                {
                    // We found the Sequences tag.

                    // For each Sequence tag in this sequence...
                    var sequencesReader = reader.ReadSubtree();
                    while (sequencesReader.ReadToFollowing("Sequence"))
                    {
                        // Create a new sequence.
                        Sequence sequence = new Sequence();
                        settings.Sequences.Add(sequence);

                        // Read the sequence tag attributes (should just be "name").
                        Dictionary<String, String> sequenceAttributes = new Dictionary<string, string>();
                        ReadAttributes(sequencesReader, sequenceAttributes);

                        if (sequenceAttributes.ContainsKey("Name"))
                        {
                            sequence.name = sequenceAttributes["Name"];
                        }
                        else
                        {
                            sequence.name = "Sequence " + settings.Sequences.Count;
                            warnings.Add("No name found for sequence " + sequence.name + ".");
                        }

                        // For each frame tag in this sequence...
                        var framesReader = reader.ReadSubtree();
                        while (framesReader.ReadToFollowing("Frame"))
                        {
                            // Create a new frame.
                            Frame frame = new Frame();
                            sequence.frames.Add(frame);

                            // Read the frame attributes from XML (name, duration)
                            Dictionary<String, String> frameAttributes = new Dictionary<string, string>();
                            ReadAttributes(framesReader, frameAttributes);

                            if (frameAttributes.ContainsKey("Name"))
                            {
                                frame.Name = frameAttributes["Name"];
                            }
                            else
                            {
                                frame.Name = "Frame " + sequence.frames.Count;
                                warnings.Add("No name found for " + frame.Name + " in sequence \"" + sequence.name + "\".");
                            }

                            if (frameAttributes.ContainsKey("Duration"))
                            {
                                frame.LengthMs = ParseU16(frameAttributes["Duration"], "Duration for frame \"" + frame.Name + "\" in sequence \"" + sequence.name + "\".", ref warnings);
                            }
                            else
                            {
                                frame.Name = "Frame " + sequence.frames.Count;
                                warnings.Add("No duration found for frame \"" + frame.Name + "\" in sequence \"" + sequence.name + "\".");
                            }

                            frame.SetTargetsFromString(reader.ReadElementContentAsString(), settings.ServoCount);
                        }
                    }
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "Script")
                {
                    // We found the <Script> tag.

                    // Get the ScriptDone attribute in to our dictionary.
                    ReadAttributes(reader, xParams);

                    // Read the script.
                    script = reader.ReadElementContentAsString();
                }
                else if (reader.NodeType == XmlNodeType.Element)
                {
                    // Read the miscellaneous parameters that come in element tags, like <NeverSuspend>false</NeverSuspend>.
                    try
                    {
                        xParams[reader.Name] = reader.ReadElementContentAsString();
                    }
                    catch (XmlException e)
                    {
                        warnings.Add("Unable to parse element \"" + reader.Name + "\": " + e.Message);
                    }
                }
            }
            reader.Close();

            //// Step 2: Put the data in to the settings object.

            try
            {
                settings.SetAndCompileScript(script);
            }
            catch (Exception e)
            {
                warnings.Add("Error compiling script from XML file: " + e.Message);
                settings.ScriptInconsistent = true;
            }

            if (AssertKey("NeverSuspend",ref xParams, ref warnings))
            {
                settings.NeverSuspend = ParseBool(xParams["NeverSuspend"], "NeverSuspend", ref warnings);
            }

            if (AssertKey("SerialMode", ref xParams, ref warnings))
            {
                switch (xParams["SerialMode"])
                {
                    default: settings.SerialMode = SerialMode.SERIAL_MODE_UART_DETECT_BAUD_RATE; break;
                    case "UART_FIXED_BAUD_RATE": settings.SerialMode = SerialMode.SERIAL_MODE_UART_FIXED_BAUD_RATE; break;
                    case "USB_DUAL_PORT": settings.SerialMode = SerialMode.SERIAL_MODE_USB_DUAL_PORT; break;
                    case "USB_CHAINED": settings.SerialMode = SerialMode.SERIAL_MODE_USB_CHAINED; break;
                }
            }

            if (AssertKey("FixedBaudRate", ref xParams, ref warnings))
            {
                settings.FixedBaudRate = ParseU32(xParams["FixedBaudRate"], "FixedBaudRate", ref warnings);
            }

            if (AssertKey("SerialTimeout", ref xParams, ref warnings))
            {
                settings.SerialTimeout = ParseU16(xParams["SerialTimeout"], "SerialTimeout", ref warnings);
            }

            if (AssertKey("EnableCRC", ref xParams, ref warnings))
            {
                settings.EnableCRC = ParseBool(xParams["EnableCRC"], "EnableCRC", ref warnings);
            }

            if (AssertKey("SerialDeviceNumber", ref xParams, ref warnings))
            {
                settings.SerialDeviceNumber =  ParseU8(xParams["SerialDeviceNumber"], "SerialDeviceNumber", ref warnings);
            }

            if (AssertKey("MiniSscOffset", ref xParams, ref warnings))
            {
                settings.MiniSscOffset = ParseU8(xParams["MiniSscOffset"], "MiniSscOffset", ref warnings);
            }

            if (AssertKey("ScriptDone", ref xParams, ref warnings))
            {
                settings.ScriptDone = ParseBool(xParams["ScriptDone"], "ScriptDone", ref warnings);
            }

            // These parameters are optional because they don't apply to all Maestros.
            if (xParams.ContainsKey("ServosAvailable"))
            {
                settings.ServosAvailable = ParseU8(xParams["ServosAvailable"], "ServosAvailable", ref warnings);
            }

            if (xParams.ContainsKey("ServoPeriod"))
            {
                settings.ServoPeriod = ParseU8(xParams["ServoPeriod"], "ServoPeriod", ref warnings);
            }

            if (xParams.ContainsKey("EnablePullups"))
            {
                settings.EnablePullups = ParseBool(xParams["EnablePullups"], "EnablePullups", ref warnings);
            }

            if (xParams.ContainsKey("MiniMaestroServoPeriod"))
            {
                settings.MiniMaestroServoPeriod = ParseU32(xParams["MiniMaestroServoPeriod"], "MiniMaestroServoPeriod", ref warnings);
            }

            if (xParams.ContainsKey("ServoMultiplier"))
            {
                settings.ServoMultiplier = ParseU16(xParams["ServoMultiplier"], "ServoMultiplier", ref warnings);
            }

            return settings;
        }

        /// <summary>
        /// If the XmlReader is at an element that has attributes, this will read all those
        /// attributes in to the dictionary.
        /// </summary>
        private static void ReadAttributes(XmlReader reader, Dictionary<String, String> attributes)
        {
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    attributes[reader.Name] = reader.ReadContentAsString();
                }
            }

            // Move back to the element (so we can call ReadSubtree if needed)
            reader.MoveToElement();
        }

        private static bool ParseBool(string input, string name, ref IList<string> warnings)
        {
            if (bool.TryParse(input, out bool result))
            {
                return result;
            }
            else
            {
                warnings.Add(name + ": Invalid integer value \"" + input + "\".");
                return default(bool);
            }
        }

        private static byte ParseU8(string input, string name, ref IList<string> warnings)
        {
            if (byte.TryParse(input, out byte result))
            {
                return result;
            }
            else
            {
                warnings.Add(name + ": Invalid integer value \"" + input + "\".");
                return default(byte);
            }
        }

        private static ushort ParseU16(string input, string name, ref IList<string> warnings)
        {
            if (ushort.TryParse(input, out ushort result))
            {
                return result;
            }
            else
            {
                warnings.Add(name + ": Invalid integer value \"" + input + "\".");
                return default(ushort);
            }
        }

        private static uint ParseU32(string input, string name, ref IList<string> warnings)
        {
            if (uint.TryParse(input, out uint result))
            {
                return result;
            }
            else
            {
                warnings.Add(name + ": Invalid integer value \"" + input + "\".");
                return default(uint);
            }
        }

        private static bool AssertKey(string key, ref Dictionary<string, string> paramsFromXml, ref IList<string> warnings)
        {
            if (paramsFromXml.ContainsKey(key))
            {
                return true;
            }
            else
            {
                warnings.Add("The " + key + " setting was missing.");
                return false;
            }
        }

        /// <summary>
        /// Saves a UscSettings object to a textfile.
        /// </summary>
        /// <param name="settings">The settings to read from.</param>
        /// <param name="sw">The file to write to.</param>
        public static void Save(UscSettings settings, StreamWriter sw)
        {
            XmlTextWriter writer = new XmlTextWriter(sw)
            {
                Formatting = Formatting.Indented
            };
            writer.WriteComment("Pololu Maestro servo controller settings file, http://www.pololu.com/catalog/product/1350");
            writer.WriteStartElement("UscSettings");
            writer.WriteAttributeString("version", "1"); // XML file version, so that we can parse old XML types in the future
            writer.WriteElementString("NeverSuspend", settings.NeverSuspend ? "true" : "false");
            writer.WriteElementString("SerialMode", settings.SerialMode.ToString().Replace("SERIAL_MODE_", ""));
            writer.WriteElementString("FixedBaudRate", settings.FixedBaudRate.ToString());
            writer.WriteElementString("SerialTimeout", settings.SerialTimeout.ToString());
            writer.WriteElementString("EnableCrc", settings.EnableCRC ? "true" : "false");
            writer.WriteElementString("SerialDeviceNumber", settings.SerialDeviceNumber.ToString());
            writer.WriteElementString("SerialMiniSscOffset", settings.MiniSscOffset.ToString());

            if (settings.ServoCount > 18)
            {
                writer.WriteElementString("EnablePullups", settings.EnablePullups ? "true" : "false");
            }

            writer.WriteStartElement("Channels");

            // Attributes of the Channels tag
            if (settings.ServoCount == 6)
            {
                writer.WriteAttributeString("ServosAvailable", settings.ServosAvailable.ToString());
                writer.WriteAttributeString("ServoPeriod", settings.ServoPeriod.ToString());
            }
            else
            {
                writer.WriteAttributeString("MiniMaestroServoPeriod", settings.MiniMaestroServoPeriod.ToString());
                writer.WriteAttributeString("ServoMultiplier", settings.ServoMultiplier.ToString());
            }
            writer.WriteComment("Period = " + (settings.PeriodInMicroseconds / 1000M).ToString() + " ms");

            for (byte i = 0; i < settings.ServoCount; i++)
            {
                ChannelSetting setting = settings.ChannelSettings[i];
                writer.WriteComment("Channel " + i.ToString());
                writer.WriteStartElement("Channel");
                writer.WriteAttributeString("name", setting.Name);
                writer.WriteAttributeString("mode", setting.Mode.ToString());
                writer.WriteAttributeString("min", setting.Minimum.ToString());
                writer.WriteAttributeString("max", setting.Maximum.ToString());
                writer.WriteAttributeString("homemode", setting.HomeMode.ToString());
                writer.WriteAttributeString("home", setting.Home.ToString());
                writer.WriteAttributeString("speed", setting.Speed.ToString());
                writer.WriteAttributeString("acceleration", setting.Acceleration.ToString());
                writer.WriteAttributeString("neutral", setting.Neutral.ToString());
                writer.WriteAttributeString("range", setting.Range.ToString());
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            writer.WriteStartElement("Sequences");
            foreach (var sequence in settings.Sequences)
            {
                writer.WriteStartElement("Sequence");
                writer.WriteAttributeString("name", sequence.name);
                foreach (Frame frame in sequence.frames)
                {
                    frame.WriteXml(writer);
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement(); // end sequences


            writer.WriteStartElement("Script");
            writer.WriteAttributeString("ScriptDone", settings.ScriptDone ? "true" : "false");
            writer.WriteString(settings.Script);
            writer.WriteEndElement(); // end script

            writer.WriteEndElement(); // End UscSettings tag.
        }
    }
}