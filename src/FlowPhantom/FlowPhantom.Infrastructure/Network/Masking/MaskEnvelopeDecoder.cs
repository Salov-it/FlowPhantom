using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Infrastructure.Network.Masking
{
    /// <summary>
    /// Декодер маскировочного фрейма.
    ///
    /// Формат:
    ///  [0..3]    MAGIC (4 bytes)
    ///  [4]       TYPE (1 byte)
    ///  [5..8]    SEGMENT_ID (int32)
    ///  [9..12]   PAYLOAD_LEN (int32)
    ///  [13..16]  PADDING_LEN (int32)
    ///  [...]     PAYLOAD
    ///  [...]     PADDING
    /// </summary>
    public static class MaskEnvelopeDecoder
    {
        private static readonly byte[] MAGIC = { 0xF1, 0x0F, 0xAA, 0x55 };

        /// <summary>
        /// Результат декодирования фрейма:
        /// - SegmentId — номер сегмента
        /// - Payload   — полезная нагрузка
        /// </summary>
        public sealed class DecodedFrame
        {
            public int SegmentId { get; }
            public byte[] Payload { get; }

            public DecodedFrame(int segmentId, byte[] payload)
            {
                SegmentId = segmentId;
                Payload = payload;
            }
        }

        /// <summary>
        /// Старый метод: возвращает только payload.
        /// Оставляем для совместимости.
        /// </summary>
        public static byte[] Decode(byte[] data)
        {
            var frame = DecodeFrame(data);
            return frame.Payload;
        }

        /// <summary>
        /// Новый метод: декодирует фрейм и возвращает
        /// и SegmentId, и Payload.
        /// </summary>
        public static DecodedFrame DecodeFrame(byte[] data)
        {
            if (data == null || data.Length < 17)
                throw new Exception("Frame too small.");

            int offset = 0;

            // 1. Проверяем MAGIC
            for (int i = 0; i < MAGIC.Length; i++)
            {
                if (data[offset + i] != MAGIC[i])
                    throw new Exception("Invalid MAGIC header.");
            }
            offset += MAGIC.Length; // +4

            // 2. TYPE
            byte frameType = data[offset++];
            if (frameType != 0x01)
                throw new Exception($"Unsupported frame type: {frameType}");

            // 3. SEGMENT_ID (int32)
            int segmentId = BitConverter.ToInt32(data, offset);
            offset += 4;

            // 4. PAYLOAD_LEN (int32)
            int payloadLength = BitConverter.ToInt32(data, offset);
            offset += 4;
            if (payloadLength < 0)
                throw new Exception("Negative payload length.");

            // 5. PADDING_LEN (int32)
            int paddingLength = BitConverter.ToInt32(data, offset);
            offset += 4;
            if (paddingLength < 0)
                throw new Exception("Negative padding length.");

            // 6. Проверяем, что данных достаточно
            int expectedSize = 4 + 1 + 4 + 4 + 4 + payloadLength + paddingLength;
            if (data.Length < expectedSize)
                throw new Exception("Frame size mismatch — data truncated.");

            // 7. Считываем PAYLOAD
            byte[] payload = new byte[payloadLength];
            Array.Copy(data, offset, payload, 0, payloadLength);

            // padding мы просто игнорируем

            return new DecodedFrame(segmentId, payload);
        }
    }
}
