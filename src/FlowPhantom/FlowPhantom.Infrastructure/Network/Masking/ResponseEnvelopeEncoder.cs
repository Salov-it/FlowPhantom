using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Infrastructure.Network.Masking
{
    public static class ResponseEnvelopeEncoder
    {
        // MAGIC для ответного фрейма, отличная от основного
        private static readonly byte[] MAGIC = { 0xA1, 0xC2, 0xB3, 0xD4 };

        /// <summary>
        /// Кодирует простой ответ сервера:
        /// [MAGIC][SegmentId:int32][MsgLen:int32][Message(bytes)]
        /// </summary>
        public static byte[] Encode(int segmentId, string message)
        {
            var msgBytes = System.Text.Encoding.UTF8.GetBytes(message);
            var segBytes = BitConverter.GetBytes(segmentId);
            var lenBytes = BitConverter.GetBytes(msgBytes.Length);

            var buffer = new byte[MAGIC.Length + 4 + 4 + msgBytes.Length];
            int o = 0;

            Array.Copy(MAGIC, 0, buffer, o, MAGIC.Length); o += MAGIC.Length;
            Array.Copy(segBytes, 0, buffer, o, 4); o += 4;
            Array.Copy(lenBytes, 0, buffer, o, 4); o += 4;
            Array.Copy(msgBytes, 0, buffer, o, msgBytes.Length);

            return buffer;
        }
    }
}
