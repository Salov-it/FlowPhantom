using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Infrastructure.Media
{
    /// <summary>
    /// Кодировщик/декодировщик уровня "MediaSegment".
    ///
    /// Формат mediaPayload:
    /// [StreamId:int32][SegmentIndex:int32][PtsMs:int32][DurationMs:int32][RawData...]
    /// </summary>
    public static class MediaSegmentCodec
    {
        private const int HeaderSize = 4 + 4 + 4 + 4; // 16 байт

        /// <summary>
        /// Кодирование: meta + rawData → mediaPayload (для MaskEnvelopeEncoder).
        /// </summary>
        public static byte[] Encode(MediaSegmentMeta meta, byte[] rawData)
        {
            if (rawData == null) throw new ArgumentNullException(nameof(rawData));

            var buffer = new byte[HeaderSize + rawData.Length];
            int o = 0;

            BitConverter.GetBytes(meta.StreamId).CopyTo(buffer, o); o += 4;
            BitConverter.GetBytes(meta.SegmentIndex).CopyTo(buffer, o); o += 4;
            BitConverter.GetBytes(meta.PtsMs).CopyTo(buffer, o); o += 4;
            BitConverter.GetBytes(meta.DurationMs).CopyTo(buffer, o); o += 4;

            Array.Copy(rawData, 0, buffer, o, rawData.Length);

            return buffer;
        }

        /// <summary>
        /// Декодирование: mediaPayload → meta + rawData.
        /// </summary>
        public static (MediaSegmentMeta meta, byte[] rawData) Decode(byte[] mediaPayload)
        {
            if (mediaPayload == null || mediaPayload.Length < HeaderSize)
                throw new Exception("Media payload too small");

            int o = 0;

            int streamId = BitConverter.ToInt32(mediaPayload, o); o += 4;
            int segmentIndex = BitConverter.ToInt32(mediaPayload, o); o += 4;
            int ptsMs = BitConverter.ToInt32(mediaPayload, o); o += 4;
            int durMs = BitConverter.ToInt32(mediaPayload, o); o += 4;

            var meta = new MediaSegmentMeta(streamId, segmentIndex, ptsMs, durMs);

            int dataLen = mediaPayload.Length - HeaderSize;
            var raw = new byte[dataLen];
            Array.Copy(mediaPayload, o, raw, 0, dataLen);

            return (meta, raw);
        }
    }
}
