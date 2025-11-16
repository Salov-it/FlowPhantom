using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Infrastructure.Network.Masking
{
    /// <summary>
    /// Кодировщик маскировочного фрейма.
    ///
    /// Формат фрейма:
    /// MAGIC (4)
    /// TYPE (1)
    /// SEGMENT_ID (4)
    /// PAYLOAD_LEN (4)
    /// PADDING_LEN (4)
    /// PAYLOAD (...)
    /// PADDING (...)
    /// </summary>
    public static class MaskEnvelopeEncoder
    {
        // Магическая сигнатура протокола (4 байта)
        private static readonly byte[] MAGIC = { 0xF1, 0x0F, 0xAA, 0x55 };

        // Глобальный Random для паддинга
        private static readonly Random Rnd = new();

        /// <summary>
        /// Упаковывает payload в маскировочный фрейм.
        /// segmentId — номер сегмента (0,1,2,...)
        /// payload   — полезные данные
        /// </summary>
        public static byte[] Encode(int segmentId, byte[] payload)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));

            // Немного "шума" для выравнивания и маскировки длины
            int paddingLen = Rnd.Next(16, 128);
            var padding = new byte[paddingLen];
            Rnd.NextBytes(padding);

            byte frameType = 0x01; // наш тип data-frame

            // Общий размер фрейма:
            // MAGIC(4) + TYPE(1) + SEGID(4) + PAYLOAD_LEN(4) + PADDING_LEN(4) + PAYLOAD + PADDING
            int totalSize =
                4 +        // MAGIC
                1 +        // TYPE
                4 +        // SEGMENT_ID
                4 +        // PAYLOAD_LEN
                4 +        // PADDING_LEN
                payload.Length +
                paddingLen;

            var buffer = new byte[totalSize];
            int o = 0;

            // MAGIC
            Array.Copy(MAGIC, 0, buffer, o, 4);
            o += 4;

            // TYPE
            buffer[o++] = frameType;

            // SEGMENT_ID
            BitConverter.GetBytes(segmentId).CopyTo(buffer, o);
            o += 4;

            // PAYLOAD_LEN
            BitConverter.GetBytes(payload.Length).CopyTo(buffer, o);
            o += 4;

            // PADDING_LEN
            BitConverter.GetBytes(paddingLen).CopyTo(buffer, o);
            o += 4;

            // PAYLOAD
            Array.Copy(payload, 0, buffer, o, payload.Length);
            o += payload.Length;

            // PADDING
            Array.Copy(padding, 0, buffer, o, paddingLen);

            return buffer;
        }
    }
}
