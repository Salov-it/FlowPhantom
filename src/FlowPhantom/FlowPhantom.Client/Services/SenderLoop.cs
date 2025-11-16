using FlowPhantom.Client.Mask;
using FlowPhantom.Client.Transport;
using FlowPhantom.Infrastructure.Media;
using FlowPhantom.Infrastructure.Network.Masking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace FlowPhantom.Client.Services
{
    /// <summary>
    /// SenderLoop — фоновая отправка VPN-сегментов через HTTP POST.
    ///
    /// Здесь выполняется:
    ///   PacketFramer → MediaSegmentCodec → MaskEnvelopeEncoder → HTTP POST
    ///
    /// То есть ПОЛНОСТЬЮ тот же формат, что и у TCP FlowClient.
    /// </summary>
    public static class SenderLoop
    {
        private const int MaxChunkSize = 1200;

        private const int MinDelayMs = 40;
        private const int MaxDelayMs = 220;
        private static readonly Random _rnd = new();

        public static async Task Run(
            HttpClient http,
            string endpoint,
            Channel<byte[]> queue,
            Func<bool> alive)
        {
            int streamId = 1;
            int segmentIndex = 0;
            ushort sessionId = 1;

            while (alive())
            {
                // 1. Забираем пакет (сырой IP-пакет)
                byte[] packet = await queue.Reader.ReadAsync();

                // 2. Разбиваем на чанки (как видео-сегменты)
                foreach (var chunk in Chunker.Split(packet, MaxChunkSize))
                {
                    //
                    // =============================================================
                    // 3. ВНУТРЕННИЙ ФРЕЙМ (PacketFramer)
                    // =============================================================
                    //
                    var framed = PacketFramer.Frame(
                        sessionId,
                        chunk,
                        flags: 0
                    );

                    //
                    // =============================================================
                    // 4. MEDIA SEGMENT
                    // =============================================================
                    //
                    var meta = new MediaSegmentMeta(
                        streamId,
                        segmentIndex++,
                        ptsMs: 0,
                        durationMs: 0
                    );

                    var mediaPayload = MediaSegmentCodec.Encode(meta, framed);

                    //
                    // =============================================================
                    // 5. VK-MASK (MaskEnvelopeEncoder)
                    // =============================================================
                    //
                    var masked = MaskEnvelopeEncoder.Encode(
                        meta.SegmentIndex,     // segmentId
                        mediaPayload           // payload
                    );

                    //
                    // =============================================================
                    // 6. ОТПРАВКА ЧЕРЕЗ HTTP POST
                    // =============================================================
                    //
                    try
                    {
                        await http.PostAsync(endpoint, new ByteArrayContent(masked));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[SENDER] HTTP POST failed: " + ex.Message);
                        await Task.Delay(200);
                        continue;
                    }

                    //
                    // =============================================================
                    // 7. ИМИТАЦИЯ ВИДЕО-ТРАФИКА (рандомный интервал)
                    // =============================================================
                    //
                    int delay = _rnd.Next(MinDelayMs, MaxDelayMs);
                    await Task.Delay(delay);
                }
            }
        }
    }
}
