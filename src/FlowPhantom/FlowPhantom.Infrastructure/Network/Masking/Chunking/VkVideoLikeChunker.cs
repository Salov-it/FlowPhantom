using FlowPhantom.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Infrastructure.Network.Masking.Chunking
{
    /// <summary>
    /// Реалистичный чанкер, имитирующий разбиение данных
    /// на сегменты, похожие на сегменты видеопотока VK.
    ///
    /// Почему это важно:
    /// -----------------
    /// Видеоплееры (VK, YouTube) загружают контент кусками:
    ///   - первые чанки маленькие (быстрый старт)
    ///   - потом чанки ~200-500 KB
    ///   - иногда крупные (до ~1 MB)
    ///
    /// Такой паттерн должен сбивать DPI.
    /// </summary>
    public class VkVideoLikeChunker : IChunker
    {
        private readonly Random _rnd = new();

        // Размеры сегментов как у настоящих CDN VK
        private readonly int _initialMin = 40_000;   // быстрый старт: 40–90 KB
        private readonly int _initialMax = 90_000;

        private readonly int _normalMin = 180_000;   // нормальные чанки 180–420 KB
        private readonly int _normalMax = 420_000;

        private readonly int _rarePeakMin = 600_000; // редкие крупные сегменты 600KB–1MB
        private readonly int _rarePeakMax = 1_000_000;

        public IEnumerable<byte[]> Chunkify(byte[] data)
        {
            int offset = 0;
            int chunkIndex = 0;

            while (offset < data.Length)
            {
                int chunkSize;

                // 🔥 Первые 2–3 сегмента: маленькие (как у видео-старта)
                if (chunkIndex < 3)
                {
                    chunkSize = _rnd.Next(_initialMin, _initialMax);
                }
                // 🔥 Иногда делаем "крупный" сегмент — для реалистичности
                else if (_rnd.NextDouble() < 0.10) // 10% шанс
                {
                    chunkSize = _rnd.Next(_rarePeakMin, _rarePeakMax);
                }
                // 🔥 Нормальный случай
                else
                {
                    chunkSize = _rnd.Next(_normalMin, _normalMax);
                }

                int remaining = data.Length - offset;

                // последний чанк может быть меньше
                chunkSize = Math.Min(chunkSize, remaining);

                var chunk = new byte[chunkSize];
                Buffer.BlockCopy(data, offset, chunk, 0, chunkSize);

                offset += chunkSize;
                chunkIndex++;

                yield return chunk;
            }
        }
    }
}
