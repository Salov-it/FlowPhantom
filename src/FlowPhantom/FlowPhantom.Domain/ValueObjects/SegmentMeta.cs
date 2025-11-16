using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Domain.ValueObjects
{
    /// <summary>
    /// Метаданные видеосегмента —
    /// это то, что реально используется VK CDN:
    ///
    /// - Диапазон байт (Range)
    /// - Время запроса
    /// - ID условного "видео файла"
    ///
    /// Позже мы будем формировать Range-запросы
    /// "bytes=100000-200000", как у VK.
    /// </summary>
    public class SegmentMeta
    {
        public long Start { get; }
        public long End { get; }
        public string SegmentId { get; }

        public SegmentMeta(long start, long end, string segmentId)
        {
            Start = start;
            End = end;
            SegmentId = segmentId;
        }

        public long Length => End - Start;

        public override string ToString() => $"{SegmentId}: {Start}-{End}";
    }
}
