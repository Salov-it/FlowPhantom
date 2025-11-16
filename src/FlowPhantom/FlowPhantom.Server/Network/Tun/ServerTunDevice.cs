using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Server.Network.Tun
{
    /// <summary>
    /// ServerTunDevice — обёртка над Linux TUN-интерфейсом (/dev/net/tun).
    ///
    /// Работает с "сырыми" IPv4/IPv6 пакетами:
    ///   - ReadPacketAsync: читаем IP-пакет из TUN (от ядра Linux)
    ///   - WritePacketAsync: пишем IP-пакет в TUN (в ядро)
    ///
    /// Требует Linux + /dev/net/tun + root.
    /// </summary>
    public sealed class ServerTunDevice : IDisposable
    {
        private const string TunDevicePath = "/dev/net/tun";

        private const short IFF_TUN = 0x0001;
        private const short IFF_NO_PI = 0x1000;

        private const uint TUNSETIFF = 0x400454CA;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct IfReq
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string ifr_name;

            public short ifr_flags;

            // Паддинг, чтобы размер ifreq совпал с нативным
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
            public byte[] ifr_ifru;
        }

        [DllImport("libc", SetLastError = true)]
        private static extern int ioctl(int fd, uint request, ref IfReq ifr);

        public string Name { get; }

        private readonly SafeFileHandle _handle;
        private readonly FileStream _stream;

        public ServerTunDevice(string tunName = "phantom0")
        {
            Name = tunName;

            // 1) Открываем /dev/net/tun как обычный FileStream
            //    Важно: FileShare.ReadWrite, чтобы ядро не ругалось.
            _stream = new FileStream(
                TunDevicePath,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.ReadWrite,
                bufferSize: 16 * 1024,
                options: FileOptions.Asynchronous
            );

            _handle = _stream.SafeFileHandle;

            if (_handle.IsInvalid)
                throw new Exception("Failed to open /dev/net/tun (SafeFileHandle invalid).");

            int fd = _handle.DangerousGetHandle().ToInt32();

            // 2) Готовим ifreq для создания/привязки TUN-интерфейса
            var ifr = new IfReq
            {
                ifr_name = tunName,
                ifr_flags = (short)(IFF_TUN | IFF_NO_PI),
                ifr_ifru = new byte[24]
            };

            int res = ioctl(fd, TUNSETIFF, ref ifr);
            if (res < 0)
            {
                int err = Marshal.GetLastWin32Error();
                _stream.Dispose();
                throw new Exception($"ioctl(TUNSETIFF) failed, errno={err}");
            }

            Console.WriteLine($"[TUN] Server TUN '{tunName}' created and ready.");
        }

        /// <summary>
        /// Читает один IP-пакет из TUN (блокирующе/асинхронно).
        /// Возвращает null, если TUN закрыт.
        /// </summary>
        public async Task<byte[]?> ReadPacketAsync(CancellationToken ct = default)
        {
            var buffer = new byte[65535]; // максимум под IP

            int read = await _stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct);
            if (read <= 0)
                return null;

            var result = new byte[read];
            Buffer.BlockCopy(buffer, 0, result, 0, read);
            return result;
        }

        /// <summary>
        /// Пишет сырые IP-байты в TUN (ядро считает, что этот пакет пришёл "снаружи").
        /// </summary>
        public Task WritePacketAsync(byte[] packet, CancellationToken ct = default)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            return _stream.WriteAsync(packet.AsMemory(0, packet.Length), ct).AsTask();
        }

        public void Dispose()
        {
            try
            {
                _stream?.Dispose();
            }
            catch { /* ignore */ }

            try
            {
                if (!_handle.IsInvalid)
                    _handle.Dispose();
            }
            catch { /* ignore */ }
        }
    }
}
