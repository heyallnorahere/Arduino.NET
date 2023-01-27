using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Arduino.NET.Example
{
    internal static class Program
    {
        public static async Task Main(string[] args)
        {
            using var arduino = await ArduinoDevice.ConnectAsync(new Dictionary<string, string>
            {
                ["linux"] = "/dev/ttyACM0"
            }, 9600);

            if (arduino == null)
            {
                return;
            }

            var encoding = Encoding.ASCII;
            string received = string.Empty;

            arduino.OnDataReceived += async content => 
            {
                string decoded = encoding.GetString(content);
                Console.Write(decoded);

                received += decoded;
                if (received.EndsWith("Please give me data!\r\n"))
                {
                    received = string.Empty;
                    await arduino.WriteAsync("Hello!", encoding);
                    await arduino.FlushAsync();
                }
            };

            while (true)
            {
                await arduino.ReadAsync();
            }
        }
    }
}