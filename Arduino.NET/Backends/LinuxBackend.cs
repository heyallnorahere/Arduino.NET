using System;
using System.Text;
using System.Threading.Tasks;

using Tmds.Linux;
using static Tmds.Linux.LibC;

namespace Arduino.NET.Backends
{
    [BackendPlatform("linux")]
    internal sealed class LinuxBackend : IBackend
    {
        public LinuxBackend()
        {
            mFileDescriptor = -1;
            mEncoding = Encoding.ASCII;
        }

        ~LinuxBackend() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            // todo: disconnect
        }

        public bool IsConnected => mFileDescriptor >= 0;

        private unsafe int OpenFileDescriptor(string file)
        {
            var bytes = mEncoding.GetBytes(file);
            fixed (byte* ptr = bytes)
            {
                return open(ptr, O_RDWR);
            }
        }

        private unsafe int GetFileDescriptorAttributes(int fd, out termios attributes)
        {
            var tty = new termios();
            int result = tcgetattr(fd, &tty);

            attributes = tty;
            return result;
        }

        private unsafe int SetFileDescriptorAttributes(int fd, termios attributes)
        {
            return tcsetattr(fd, 0, &attributes);
        }

        private unsafe void ConfigureFileDescriptor(ref termios tty, int baudRate)
        {
            // control modes
            tty.c_cflag &= ~PARENB;
            tty.c_cflag &= ~CSTOPB;
            tty.c_cflag &= ~CSIZE;
            tty.c_cflag |= CS8;
            tty.c_cflag &= ~CRTSCTS;
            tty.c_cflag |= CREAD | CLOCAL;

            // local modes
            tty.c_lflag &= ~ICANON;
            tty.c_lflag &= ~ECHO;
            tty.c_lflag &= ~ECHOE;
            tty.c_lflag &= ~ECHONL;
            tty.c_lflag &= ~ISIG;

            // input modes
            tty.c_iflag &= ~(IXON | IXOFF | IXANY);
            tty.c_iflag &= ~(IGNBRK | BRKINT | PARMRK | ISTRIP | INLCR | IGNCR | ICRNL);

            // output modes
            tty.c_oflag &= ~OPOST;
            tty.c_oflag &= ~ONLCR;

            // no blocking
            tty.c_cc[VMIN] = 1;
            tty.c_cc[VTIME] = 0;

            fixed (termios* ptr = &tty)
            {
                cfsetispeed(ptr, (uint)baudRate);
                cfsetospeed(ptr, (uint)baudRate);
            }
        }

        public async Task<bool> ConnectAsync(string identifier, int baudRate)
        {
            if (IsConnected)
            {
                return true;
            }

            int fd = await Task.Run(() => OpenFileDescriptor(identifier));
            if (fd < 0)
            {
                return false;
            }

            if (GetFileDescriptorAttributes(fd, out termios tty) != 0)
            {
                close(fd);
                return false;
            }

            ConfigureFileDescriptor(ref tty, baudRate);
            if (SetFileDescriptorAttributes(fd, tty) != 0)
            {
                close(fd);
                return false;
            }

            mFileDescriptor = fd;
            return true;
        }

        private unsafe bool ReadFileDescriptor(Action<byte[]> callback)
        {
            var buffer = new byte[256];
            fixed (byte* ptr = buffer)
            {
                ssize_t bytesRead = read(mFileDescriptor, ptr, (size_t)buffer.Length);
                if (bytesRead > 0)
                {
                    callback(buffer[0..(int)bytesRead]);
                    return true;
                }
            }

            return false;
        }

        public async Task<bool> ReadAsync(Action<byte[]> callback)
        {
            if (!IsConnected)
            {
                return false;
            }

            return await Task.Run(() => ReadFileDescriptor(callback));
        }

        private unsafe bool WriteFileDescriptor(byte[] content)
        {
            fixed (byte* ptr = content)
            {
                ssize_t bytesWritten = write(mFileDescriptor, ptr, content.Length);
                return bytesWritten > 0;
            }
        }

        public async Task<bool> WriteAsync(byte[] content)
        {
            if (!IsConnected)
            {
                return false;
            }

            return await Task.Run(() => WriteFileDescriptor(content));
        }

        private int mFileDescriptor;
        private readonly Encoding mEncoding;
    }
}