using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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

        private static ConstructorInfo? FindViableConstructor()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var types = assembly.GetTypes();

            foreach (var type in types)
            {
                if (sBackendConstructors.ContainsKey(type))
                {
                    return sBackendConstructors[type];
                }

                var interfaces = type.GetInterfaces();
                if (!interfaces.Contains(typeof(IBackend)))
                {
                    continue;
                }

                var attribute = type.GetCustomAttribute<BackendPlatformAttribute>();
                if (attribute == null)
                {
                    continue;
                }

                var platform = OSPlatform.Create(attribute.Platform);
                if (!RuntimeInformation.IsOSPlatform(platform))
                {
                    continue;
                }

                var constructor = type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, Array.Empty<Type>());
                if (constructor == null)
                {
                    continue;
                }

                sBackendConstructors.Add(type, constructor);
                return constructor;
            }

            return null;
        }

        public static ArduinoDevice? Create()
        {
            var constructor = FindViableConstructor();
            if (constructor == null)
            {
                return null;
            }

            var instance = (IBackend)constructor.Invoke(null);
            return new ArduinoDevice(instance);
        }

        private ArduinoDevice(IBackend backend)
        {
            mBackend = backend;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            mBackend.Dispose();
        }

        public async Task ReadAsync()
        {
            await mBackend.ReadAsync(content => OnDataReceived?.Invoke(content));
        }

        public async Task WriteAsync(byte[] content)
        {
            await mBackend.WriteAsync(content);
        }

        internal IBackend Backend => mBackend;
        public bool IsConnected => mBackend.IsConnected;

        public event Action<byte[]>? OnDataReceived;

        private readonly IBackend mBackend;
    }
}