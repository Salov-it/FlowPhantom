using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Infrastructure.Media
{
    /// <summary>
    /// Метаданные "видеосегмента".
    /// 
    /// Это логический слой:
    /// - streamId      — идентификатор потока (можно оставить 1)
    /// - segmentIndex  — номер сегмента (0, 1, 2, 3, ...)
    /// - ptsMs         — "время проигрывания" в миллисекундах (presentation timestamp)
    /// - durationMs    — длительность сегмента (условная), например 200–400 мс
    /// </summary>
    public sealed class MediaSegmentMeta
    {
        public int StreamId { get; }
        public int SegmentIndex { get; }
        public int PtsMs { get; }
        public int DurationMs { get; }

        public MediaSegmentMeta(int streamId, int segmentIndex, int ptsMs, int durationMs)
        {
            StreamId = streamId;
            SegmentIndex = segmentIndex;
            PtsMs = ptsMs;
            DurationMs = durationMs;
        }

        public override string ToString()
            => $"Stream={StreamId}, Seg={SegmentIndex}, PTS={PtsMs}ms, Dur={DurationMs}ms";
    }
}
