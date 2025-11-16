using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Server.Network
{
    /// <summary>
    /// UdpNatRouter — минимальный userspace NAT для UDP.
    /// 
    /// ⚠ MVP:
    ///  - принимает IPv4+UDP пакет от клиента (из TUN),
    ///  - парсит его,
    ///  - отправляет payload во внешний интернет через UdpClient,
    ///  - ждёт один ответ,
    ///  - строит обратный IPv4+UDP пакет и возвращает его.
    ///
    /// Это позволяет, например, гонять DNS-запросы или другие UDP-сервисы.
    /// </summary>
    public class UdpNatRouter
    {
        private readonly UdpClient _udpClient;

        public UdpNatRouter()
        {
            // 0.0.0.0:0 — пусть ОС выберет исходный порт
            _udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
        }

        /// <summary>
        /// Принимает исходный IPv4+UDP пакет, отправляет payload в интернет, возвращает IPv4+UDP ответ.
        /// </summary>
        public async Task<byte[]?> ForwardAsync(byte[] ipPacket, int timeoutMs = 3000)
        {
            var parsed = IpPacket.TryParseUdp(ipPacket);
            if (parsed == null)
            {
                Console.WriteLine("[NAT] Not IPv4/UDP packet or invalid.");
                return null;
            }

            // Отправляем payload на parsed.DstIp:parsed.DstPort
            var remote = new IPEndPoint(parsed.DstIp, parsed.DstPort);
            await _udpClient.SendAsync(parsed.Payload, parsed.Payload.Length, remote);

            // Ждём один ответ
            using var cts = new CancellationTokenSource(timeoutMs);

            try
            {
                var receiveTask = _udpClient.ReceiveAsync(cts.Token);
                var result = await receiveTask;

                byte[] responsePayload = result.Buffer;

                // Строим IPv4+UDP пакет-ответ назад к TUN-клиенту
                var responsePacket = IpPacket.BuildUdpResponsePacket(parsed, responsePayload);
                return responsePacket;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[NAT] Timeout waiting for UDP response.");
                return null;
            }
        }
    }
}
