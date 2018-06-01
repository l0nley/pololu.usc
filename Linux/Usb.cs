using System;
using System.Collections.Generic;
using System.IO;

namespace Pololu.Usc.Linux
{
    public static class Usb
    {
        public static int WM_DEVICECHANGE { get { return 0; } }

        public static bool SupportsNotify { get { return false; } }

        public static IntPtr NotificationRegister(Guid guid, IntPtr handle)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns a list of port names (e.g. "COM2", "COM3") for all
        /// ACM USB serial ports.  Ignores the deviceInstanceIdPrefix argument. 
        /// </param>
        /// <returns></returns>
        public static IList<String> GetPortNames(String deviceInstanceIdPrefix)
        {
            IList<String> l = new List<String>();
            foreach (string s in Directory.GetFiles("/dev/"))
            {
                if (s.StartsWith("/dev/ttyACM") || s.StartsWith("/dev/ttyUSB"))
                    l.Add(s);
            }
            return l;
        }

        public static void Check()
        {
            LibUsb.HandleEvents();
        }
    }
}