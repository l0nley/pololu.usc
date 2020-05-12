using Pololu.Usc;
using System;

namespace Polulu.Usc.Cmd
{
    class Program
    {
        static void Main(string[] args)
        {
            var devices = MaestroController.GetDevices();
            foreach(var device in devices)
            {

            }
        }
    }
}
