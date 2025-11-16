using FlowPhantom.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Domain.Entities
{
    /// <summary>
    /// Результат применения маскирующего слоя:
    /// VPN-чунк превращается в "видеосегмент".
    ///
    /// Это именно то, что клиент отправляет на сервер.
    /// </summary>
    public class MaskedSegment
    {
        public ChunkId ChunkId { get; }
        public SegmentMeta Meta { get; }
        public byte[] MaskedData { get; }

        public MaskedSegment(ChunkId id, SegmentMeta meta, byte[] maskedData)
        {
            ChunkId = id;
            Meta = meta;
            MaskedData = maskedData;
        }

        public int Size => MaskedData.Length;
    }
}
