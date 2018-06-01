using System;
using System.Text;
using System.Xml;

namespace Pololu.Usc
{
    public class Frame
    {
        public string Name { get; set; }
        public ushort LengthMs { get; set; }

        /// <summary>
        /// Gets the target of the given channel.
        /// </summary>
        /// <remarks>
        /// By retreiving targets this way, we protect the application against
        /// any kind of case where the Frame object might have fewer targets
        /// than expected.
        /// </remarks>
        [System.Runtime.CompilerServices.IndexerName("target")]
        public ushort this[int channel]
        {
            get
            {
                if (Targets == null || channel >= Targets.Length)
                {
                    return 0;
                }

                return Targets[channel];
            }
        }

        public ushort[] Targets { get; set; }

        /// <summary>
        /// Returns a string with all the servo positions, separated by spaces,
        /// e.g. "0 0 4000 0 1000 0 0".
        /// </summary>
        /// <returns></returns>
        public string GetTargetsString()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < Targets.Length; i++)
            {
                if (i != 0)
                {
                    sb.Append(" ");
                }

                sb.Append(Targets[i].ToString());
            }

            return sb.ToString();
        }

        /// <summary>
        /// Take a (potentially malformed) string with target numbers separated by spaces
        /// and use it to set the targets.
        /// </summary>
        /// <param name="targetsString"></param>
        /// <param name="servoCount"></param>
        public void SetTargetsFromString(string targetsString, byte servoCount)
        {
            ushort[] tmpTargets = new ushort[servoCount];

            string[] targetStrings = targetsString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < targetStrings.Length && i < servoCount; i++)
            {
                try
                {
                    tmpTargets[i] = ushort.Parse(targetStrings[i]);
                }
                catch { }
            }
            Targets = tmpTargets;
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("Frame");
            writer.WriteAttributeString("name", Name);
            writer.WriteAttributeString("duration", LengthMs.ToString());
            writer.WriteString(GetTargetsString());
            writer.WriteEndElement();
        }
    }
}