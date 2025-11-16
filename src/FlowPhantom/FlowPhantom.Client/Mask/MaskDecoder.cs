using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Client.Mask
{

    /// <summary>
    /// MaskDecoder — обратная операция к MaskEncoder.
    ///
    /// Читает нашу псевдо-видео-структуру:
    ///   [CodecProfile:1]
    ///   [QualityCode:1]
    ///   [SegmentId:4]
    ///   [PayloadLength:2]
    ///   [PaddingLength:2]
    ///   [Payload...]
    ///   [Padding...]
    ///
    /// И возвращает только Payload.
    /// </summary>
    public static class MaskDecoder
    {
        public static byte[] Decode(byte[] masked)
        {
            const int headerSize = 10;

            if (masked.Length < headerSize)
                return Array.Empty<byte>();

            int offset = 0;

            // CodecProfile
            byte codecProfile = masked[offset++];
            // QualityCode
            byte qualityCode = masked[offset++];

            // SegmentId
            uint segmentId = BinaryPrimitives.ReadUInt32BigEndian(masked.AsSpan(offset, 4));
            offset += 4;

            // PayloadLength
            ushort payloadLength = BinaryPrimitives.ReadUInt16BigEndian(masked.AsSpan(offset, 2));
            offset += 2;

            // PaddingLength
            ushort paddingLength = BinaryPrimitives.ReadUInt16BigEndian(masked.AsSpan(offset, 2));
            offset += 2;

            // Валидация
            if (offset + payloadLength + paddingLength > masked.Length)
                return Array.Empty<byte>();

            // Вытаскиваем полезную нагрузку
            byte[] payload = new byte[payloadLength];
            Buffer.BlockCopy(masked, offset, payload, 0, payloadLength);

            // padding нам не нужен, просто пропускаем
            // offset += payloadLength + paddingLength;

            return payload;
        }
    }
}
