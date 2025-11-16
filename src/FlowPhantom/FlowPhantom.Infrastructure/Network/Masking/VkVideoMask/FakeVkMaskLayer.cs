using FlowPhantom.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Infrastructure.Network.Masking.VkVideoMask
{
    /// <summary>
    /// Упрощённый маскирующий слой, который добавляет поверх чанка
    /// фейковые HTTP-заголовки видеосегмента VK.
    ///
    /// Это полезно для тестов и проверки end-to-end цепочки:
    ///   Chunk → Mask → Server → Demask → Forwarder
    ///
    /// Позже заменим на полноценный:
    ///   - HTTP/3 framing
    ///   - QUIC packet simulation
    ///   - JA3 fingerprint
    /// </summary>
    public class FakeVkMaskLayer : IMaskLayer
    {
        private readonly Random _rnd = new();

        public Task<byte[]> ApplyMaskAsync(byte[] chunk, CancellationToken ct = default)
        {
            // генерируем фейковый "range"
            long start = _rnd.Next(0, 5_000_000);
            long end = start + chunk.Length - 1;

            // рандомный id сегмента
            int segmentId = _rnd.Next(1000, 9999);

            // заголовки, похожие на VK CDN
            var header = new StringBuilder();
            header.AppendLine($"GET /video/seg-{segmentId}.mp4 HTTP/1.1");
            header.AppendLine("Host: vkvideo.ru");
            header.AppendLine("User-Agent: VKClient/7.33 (Android 13)");
            header.AppendLine("Accept: */*");
            header.AppendLine($"Range: bytes={start}-{end}");
            header.AppendLine(); // пустая строка — разделитель
                                 // после пустой строки идёт тело сегмента

            byte[] headerBytes = Encoding.UTF8.GetBytes(header.ToString());

            // Склеиваем данные как:
            // [FAKE HTTP HEADER]
            // [RAW VPN DATA INSIDE THE VIDEO BODY]
            byte[] masked = headerBytes.Concat(chunk).ToArray();

            return Task.FromResult(masked);
        }
    }
}
