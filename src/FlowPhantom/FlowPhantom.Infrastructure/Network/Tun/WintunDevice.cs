using FlowPhantom.Infrastructure.Network.Tun.Wintun;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Infrastructure.Network.Tun
{
    public sealed class WintunDevice : IDisposable
    {
        private readonly IntPtr _adapter;
        private readonly IntPtr _session;

        private bool _running;

        public event Action<byte[]>? OnPacket;

        public WintunDevice(string name = "FlowPhantom", string type = "FlowPhantomTun")
        {
            // Создаем адаптер
            _adapter = WintunApi.WintunCreateAdapter(name, type, IntPtr.Zero);
            if (_adapter == IntPtr.Zero)
                throw new Exception("Failed to create Wintun adapter.");

            // Стартуем сессию (2MB ring)
            _session = WintunApi.WintunStartSession(_adapter, 0x200000);
            if (_session == IntPtr.Zero)
                throw new Exception("Failed to start Wintun session.");
        }

        public void Start()
        {
            if (_running)
                return;

            _running = true;
            Task.Run(ReadLoop);
        }

        private unsafe void ReadLoop()
        {
            while (_running)
            {
                try
                {
                    uint size;
                    IntPtr packet = WintunApi.WintunReceivePacket(_session, out size);

                    if (packet == IntPtr.Zero)
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                    var buffer = new byte[size];
                    Marshal.Copy(packet, buffer, 0, (int)size);

                    WintunApi.WintunReleaseReceivePacket(_session, packet);

                    OnPacket?.Invoke(buffer);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[TUN] Read error: " + ex.Message);
                }
            }
        }

        public void WritePacket(byte[] data)
        {
            unsafe
            {
                IntPtr packet = WintunApi.WintunAllocateSendPacket(_session, (uint)data.Length);
                Marshal.Copy(data, 0, packet, data.Length);
                WintunApi.WintunSendPacket(_session, packet);
            }
        }

        public void Stop()
        {
            _running = false;
        }

        public void Dispose()
        {
            Stop();
            WintunApi.WintunEndSession(_session);
            WintunApi.WintunDeleteAdapter(_adapter);
        }
    }
}


