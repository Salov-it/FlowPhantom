using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Domain.Protocol
{
    /// <summary>
    /// Описание паттерна поведения "видеосегмента":
    /// - примерный размер
    /// - минимальная и максимальная длина
    /// - паузы между сегментами
    ///
    /// Domain описывает модель, Infrastructure — реализацию.
    /// </summary>
    public class VideoMaskPattern
    {
        public int MinChunkSize { get; }
        public int MaxChunkSize { get; }
        public int MinDelayMs { get; }
        public int MaxDelayMs { get; }

        public VideoMaskPattern(int minChunkSize, int maxChunkSize, int minDelayMs, int maxDelayMs)
        {
            MinChunkSize = minChunkSize;
            MaxChunkSize = maxChunkSize;
            MinDelayMs = minDelayMs;
            MaxDelayMs = maxDelayMs;
        }

        public override string ToString() =>
            $"Chunks: {MinChunkSize}-{MaxChunkSize} bytes | Delay: {MinDelayMs}-{MaxDelayMs}ms";
    }
}
