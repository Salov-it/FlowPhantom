using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Client.Transport
{
    // <summary>
    /// Фрейм: [Length(2)][Flags(1)][SessionId(2)][Payload]
    /// Служит базой для MUX.
    /// </summary>
    public static class PacketFramer
    {
        public static byte[] Frame(ushort sessionId, byte[] payload, byte flags = 0)
        {
            ushort len = (ushort)payload.Length;
            byte[] frame = new byte[5 + payload.Length];

            BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(0, 2), len);  // Length
            frame[2] = flags;                                                // Flags
            BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(3, 2), sessionId); // SessionId

            Buffer.BlockCopy(payload, 0, frame, 5, payload.Length);

            return frame;
        }

        public static bool TryParse(ReadOnlySpan<byte> data, out ushort sessionId, out byte flags, out byte[] payload)
        {
            sessionId = 0;
            flags = 0;
            payload = Array.Empty<byte>();

            if (data.Length < 5)
                return false;

            var len = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(0, 2));
            flags = data[2];
            sessionId = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(3, 2));

            if (data.Length - 5 < len)
                return false;

            payload = data.Slice(5, len).ToArray();
            return true;
        }
    }
}
