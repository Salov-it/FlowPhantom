using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Infrastructure.Common.Protocol
{
    public static class PacketFramer
    {
        public static byte[] Frame(ushort sessionId, byte[] payload, byte flags = 0)
        {
            ushort length = (ushort)payload.Length;
            var buffer = new byte[5 + payload.Length];

            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(0, 2), length);
            buffer[2] = flags;
            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(3, 2), sessionId);
            payload.CopyTo(buffer, 5);

            return buffer;
        }

        public static bool TryParse(byte[] data, out ushort sessionId, out byte flags, out byte[] payload)
        {
            sessionId = 0;
            flags = 0;
            payload = Array.Empty<byte>();

            if (data.Length < 5)
                return false;

            var length = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(0, 2));
            flags = data[2];
            sessionId = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(3, 2));

            if (data.Length - 5 < length)
                return false;

            payload = data.AsSpan(5, length).ToArray();
            return true;
        }
    }
}
