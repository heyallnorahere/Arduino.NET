using System;
using System.Threading.Tasks;

namespace Arduino.NET.Example
{
    internal static class Program
    {
        private static void OnDataReceived(byte[] content)
        {
            Console.WriteLine("Data received!");
        }

        public static async Task<int> Main(string[] args)
        {
            using var arduino = ArduinoDevice.Create();
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