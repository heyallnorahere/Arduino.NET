using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Arduino.NET.Example
{
    internal static class Program
    {
        private static void OnDataReceived(byte[] content)
        {
            Console.WriteLine("Data received!");
            Console.WriteLine(Encoding.ASCII.GetString(content));
        }

        public static async Task<int> Main(string[] args)
        {
            using var arduino = await ArduinoDevice.ConnectAsync(new Dictionary<string, string>
            {
                ["linux"] = "/dev/ttyACM0"
            }, 9600);

            if (arduino == null)
            {
                return 1;
            }

            arduino.OnDataReceived += OnDataReceived;
            await arduino.ReadAsync();

            return 0;
        }
    }
}