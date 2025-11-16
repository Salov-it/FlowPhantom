using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Domain.ValueObjects
{
    /// <summary>
    /// Идентификатор чанка.
    /// ValueObject — используется как неизменяемая структура данных.
    /// </summary>
    public readonly struct ChunkId
    {
        public int Value { get; }

        public ChunkId(int value)
        {
            Value = value;
        }

        public override string ToString() => Value.ToString();
    }
}
