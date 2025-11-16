using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Domain.Entities
{
    /// <summary>
    /// Обертка для передаваемого сегмента.
    /// Это может быть:
    /// - XOR обфускация
    /// - Noise Protocol frame
    /// - QUIC/HTTP3 упаковка
    ///
    /// Domain описывает структуру, но не реализацию.
    /// </summary>
    public class MaskEnvelope
    {
        public byte[] Payload { get; }
        public byte[]? Padding { get; }

        public MaskEnvelope(byte[] payload, byte[]? padding = null)
        {
            Payload = payload;
            Padding = padding;
        }

        public int TotalSize => Payload.Length + (Padding?.Length ?? 0);
    }
}
