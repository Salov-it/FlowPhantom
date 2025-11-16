using FlowPhantom.Client.Transport;
using FlowPhantom.Infrastructure.Media;
using FlowPhantom.Infrastructure.Network.Masking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Client.Services
{
    /// <summary>
    /// ReceiverLoop — универсальный обработчик входящих "маскированных" ответов.
    ///
    /// Работает с протоколом:
    ///   Mask → MediaSegment → PacketFramer → Payload(IP)
    ///
    /// Используется в HTTP-long-polling или WebSocket режиме.
    /// </summary>
    public static class ReceiverLoop
    {
        public static async Task Run(
            HttpClient http,
            string endpoint,
            Action<byte[]> onPayload,  // готовые IP-пакеты
            Func<bool> alive)
        {
            while (alive())
            {
                HttpResponseMessage response;

                try
                {
                    // long-poll GET
                    response = await http.GetAsync(endpoint, HttpCompletionOption.ResponseContentRead);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[CLIENT][Receiver] HTTP error: " + ex.Message);
                    await Task.Delay(200);
                    continue;
                }

                var raw = await response.Content.ReadAsByteArrayAsync();
                if (raw == null || raw.Length < 4)
                {
                    // Пустой ответ — ждём дальше
                    await Task.Delay(50);
                    continue;
                }

                try
                {
                    // 1) Маскированная VK-video оболочка → mediaPayload
                    var maskFrame = MaskEnvelopeDecoder.DecodeFrame(raw);

                    // 2) MediaSegment → meta + innerFrame
                    var (meta, innerFrame) = MediaSegmentCodec.Decode(maskFrame.Payload);

                    // 3) innerFrame = PacketFramer.Frame (...)
                    if (!PacketFramer.TryParse(innerFrame, out ushort sessionId, out byte flags, out var payload))
                    {
                        Console.WriteLine("[CLIENT][Receiver] PacketFramer parse error");
                        continue;
                    }

                    // 4) payload = IP-пакет или пользовательские данные
                    onPayload(payload);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[CLIENT][Receiver] Decode error: " + ex.Message);
                }
            }
        }
    }
}