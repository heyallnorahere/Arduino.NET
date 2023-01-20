using System;
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

        public Task ReadAsync(Action<byte[]> callback);
        public Task WriteAsync(byte[] content);
    }
}