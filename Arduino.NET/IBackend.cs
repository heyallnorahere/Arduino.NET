using System;
using System.Threading;
using System.Threading.Tasks;

namespace Arduino.NET
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal sealed class BackendPlatformAttribute : Attribute
    {
        public BackendPlatformAttribute(string platform)
        {
            Platform = platform;
        }

        public string Platform { get; }
    }

    internal interface IBackend : IDisposable
    {
        public bool IsConnected { get; }

        public Task<bool> ConnectAsync(string identifier, int baudRate);

        public bool Read(Action<byte[]> callback);
        public Task<bool> ReadAsync(Action<byte[]> callback, CancellationToken? token);

        public bool Write(byte[] content);
        public Task<bool> WriteAsync(byte[] content, CancellationToken? token);

        public bool Flush();
        public Task<bool> FlushAsync(CancellationToken? token);
    }
}