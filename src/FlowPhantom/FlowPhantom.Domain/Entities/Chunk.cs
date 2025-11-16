using FlowPhantom.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Domain.Entities
{
    /// <summary>
    /// Чанк — это кусок исходного VPN-трафика,
    /// который будет маскироваться под видеосегмент.
    ///
    /// Он является частью исходного потока, поэтому
    /// содержит номер/идентификатор и данные.
    /// </summary>
    public class Chunk
    {
        public ChunkId Id { get; }
        public byte[] Data { get; }

        public Chunk(ChunkId id, byte[] data)
        {
            Id = id;
            Data = data;
        }

        public int Size => Data.Length;
    }
}
