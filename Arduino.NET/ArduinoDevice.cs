using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Arduino.NET
{
    public sealed class ArduinoDevice : IDisposable
    {
        private static readonly Dictionary<Type, ConstructorInfo> sBackendConstructors;
        static ArduinoDevice()
        {
            sBackendConstructors = new Dictionary<Type, ConstructorInfo>();
        }

        private static ConstructorInfo? FindViableConstructor(out string platform)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var types = assembly.GetTypes();

            foreach (var type in types)
            {
                var attribute = type.GetCustomAttribute<BackendPlatformAttribute>();
                if (attribute == null)
                {
                    continue;
                }

                if (sBackendConstructors.ContainsKey(type))
                {
                    platform = attribute.Platform;
                    return sBackendConstructors[type];
                }

                var interfaces = type.GetInterfaces();
                if (!interfaces.Contains(typeof(IBackend)))
                {
                    continue;
                }


                var platformId = OSPlatform.Create(attribute.Platform);
                if (!RuntimeInformation.IsOSPlatform(platformId))
                {
                    continue;
                }

                var constructor = type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, Array.Empty<Type>());
                if (constructor == null)
                {
                    continue;
                }

                sBackendConstructors.Add(type, constructor);
                platform = attribute.Platform;
                return constructor;
            }

            platform = string.Empty;
            return null;
        }

        public static async Task<ArduinoDevice?> ConnectAsync(IReadOnlyDictionary<string, string> platformIdentifiers, int baudRate)
        {
            var constructor = FindViableConstructor(out string platform);
            if (constructor == null)
            {
                return null;
            }

            if (!platformIdentifiers.ContainsKey(platform))
            {
                return null;
            }

            var instance = (IBackend)constructor.Invoke(null);
            if (!await instance.ConnectAsync(platformIdentifiers[platform], baudRate))
            {
                return null;
            }

            return new ArduinoDevice(instance);
        }

        private ArduinoDevice(IBackend backend)
        {
            mBackend = backend;
        }

        ~ArduinoDevice() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                mBackend.Dispose();
            }
        }

        public async Task<bool> ReadAsync(CancellationToken? token = null)
        {
            return await mBackend.ReadAsync(content => OnDataReceived?.Invoke(content), token);
        }

        public bool Write(string content, Encoding? encoding = null)
        {
            var usedEncoding = encoding ?? Encoding.ASCII;
            var bytes = usedEncoding.GetBytes(content);

            return Write(bytes);
        }

        public bool Write(byte[] content)
        {
            return mBackend.Write(content);
        }

        public async Task<bool> WriteAsync(string content, Encoding? encoding = null, CancellationToken? token = null)
        {
            var usedEncoding = encoding ?? Encoding.ASCII;
            var bytes = usedEncoding.GetBytes(content);

            return await WriteAsync(bytes, token);
        }

        public async Task<bool> WriteAsync(byte[] content, CancellationToken? token = null)
        {
            return await mBackend.WriteAsync(content, token);
        }

        public bool Flush() => mBackend.Flush();
        public async Task<bool> FlushAsync(CancellationToken? token = null)
        {
            return await mBackend.FlushAsync(token);
        }

        internal IBackend Backend => mBackend;
        public bool IsConnected => mBackend.IsConnected;

        public event Action<byte[]>? OnDataReceived;

        private readonly IBackend mBackend;
    }
}