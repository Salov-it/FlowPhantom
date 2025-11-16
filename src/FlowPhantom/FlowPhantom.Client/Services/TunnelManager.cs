using FlowPhantom.Infrastructure.Network.Tun;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace FlowPhantom.Client.Services
{
    /// <summary>
    /// Туннельный менеджер.
    ///
    /// Соединяет:
    ///   WintunDevice (Infrastructure) ←→ FlowClient (Client)
    ///
    /// Поток:
    ///   ОС → TUN → очередь → FlowClient → сервер → NAT → FlowClient → TUN → ОС.
    /// </summary>
    public sealed class TunnelManager
    {
        private readonly WintunDevice _tun;
        private readonly FlowClient _client;

        private readonly Channel<byte[]> _tunToClientQueue =
            Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

        private bool _running;

        public TunnelManager(WintunDevice tun, FlowClient client)
        {
            _tun = tun;
            _client = client;

            // TUN → очередь → клиент
            _tun.OnPacket += packet =>
            {
                // сырые IPv4/IPv6 пакеты из ОС кладём в очередь
                _tunToClientQueue.Writer.TryWrite(packet);
            };

            // Клиент → TUN
            _client.OnMessage += packet =>
            {
                try
                {
                    // Пакет от сервера → в виртуальный интерфейс
                    _tun.WritePacket(packet);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TUNNEL] Error writing to TUN: {ex.Message}");
                }
            };
        }

        public void Start()
        {
            if (_running) return;
            _running = true;

            _client.Start();
            _tun.Start();

            _ = Task.Run(ProcessTunToClientLoop);
        }

        public void Stop()
        {
            if (!_running) return;
            _running = false;

            _tun.Stop();
            _client.Stop();
        }

        /// <summary>
        /// Из TUN приходят пакеты → очередь → FlowClient.
        /// </summary>
        private async Task ProcessTunToClientLoop()
        {
            while (_running)
            {
                try
                {
                    var packet = await _tunToClientQueue.Reader.ReadAsync();
                    await _client.SendAsync(packet);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[TUNNEL] Error in ProcessTunToClientLoop: " + ex.Message);
                }
            }
        }
    }
}

