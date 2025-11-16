using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Infrastructure.Network.Masking
{
    public sealed class ResponseFrame
    {
        public int SegmentId { get; }
        public string Message { get; }

        public ResponseFrame(int segmentId, string message)
        {
            SegmentId = segmentId;
            Message = message;
        }
    }

    public static class ResponseEnvelopeDecoder
    {
        private static readonly byte[] MAGIC = { 0xA1, 0xC2, 0xB3, 0xD4 };

        public static ResponseFrame Decode(byte[] data)
        {
            if (data.Length < MAGIC.Length + 8)
                throw new Exception("Response too small");

            int o = 0;
            for (int i = 0; i < MAGIC.Length; i++)
            {
                if (data[o + i] != MAGIC[i])
                    throw new Exception("Invalid response MAGIC");
            }
            o += MAGIC.Length;

            int segmentId = BitConverter.ToInt32(data, o); o += 4;
            int msgLen = BitConverter.ToInt32(data, o); o += 4;

            if (msgLen < 0 || data.Length < o + msgLen)
                throw new Exception("Invalid response length");

            var msgBytes = new byte[msgLen];
            Array.Copy(data, o, msgBytes, 0, msgLen);
            var message = System.Text.Encoding.UTF8.GetString(msgBytes);

            return new ResponseFrame(segmentId, message);
        }
    }
}
