using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Server.Network
{
    /// <summary>
    /// Утилита для разбора и сборки IPv4+UDP пакетов.
    /// 
    /// ⚠️ MVP: поддерживаем только IPv4 + UDP.
    /// </summary>
    public static class IpPacket
    {
        private const byte ProtocolUdp = 17; // UDP

        public sealed class ParsedUdp
        {
            public byte Version { get; init; }
            public byte HeaderLengthBytes { get; init; }
            public byte Ttl { get; init; }
            public IPAddress SrcIp { get; init; } = IPAddress.None;
            public IPAddress DstIp { get; init; } = IPAddress.None;
            public ushort SrcPort { get; init; }
            public ushort DstPort { get; init; }
            public byte[] Payload { get; init; } = Array.Empty<byte>();
            public byte[] OriginalPacket { get; init; } = Array.Empty<byte>();
        }

        /// <summary>
        /// Разбирает IPv4 UDP-пакет.
        /// Возвращает null, если пакет не IPv4/UDP или битый.
        /// </summary>
        public static ParsedUdp? TryParseUdp(byte[] packet)
        {
            if (packet.Length < 20)
                return null;

            byte versionIhl = packet[0];
            byte version = (byte)(versionIhl >> 4);
            byte ihl = (byte)(versionIhl & 0x0F);
            byte headerLenBytes = (byte)(ihl * 4);

            if (version != 4) return null;          // только IPv4
            if (packet.Length < headerLenBytes + 8) // заголовок IP + UDP минимум
                return null;

            byte protocol = packet[9];
            if (protocol != ProtocolUdp) return null;

            var srcIp = new IPAddress(packet.AsSpan(12, 4).ToArray());
            var dstIp = new IPAddress(packet.AsSpan(16, 4).ToArray());

            int udpOffset = headerLenBytes;
            ushort srcPort = BinaryPrimitives.ReadUInt16BigEndian(packet.AsSpan(udpOffset, 2));
            ushort dstPort = BinaryPrimitives.ReadUInt16BigEndian(packet.AsSpan(udpOffset + 2, 2));
            ushort udpLen = BinaryPrimitives.ReadUInt16BigEndian(packet.AsSpan(udpOffset + 4, 2));

            int payloadOffset = udpOffset + 8;
            int payloadLen = udpLen - 8;
            if (payloadOffset + payloadLen > packet.Length)
                return null;

            var payload = packet.AsSpan(payloadOffset, payloadLen).ToArray();

            return new ParsedUdp
            {
                Version = version,
                HeaderLengthBytes = headerLenBytes,
                Ttl = packet[8],
                SrcIp = srcIp,
                DstIp = dstIp,
                SrcPort = srcPort,
                DstPort = dstPort,
                Payload = payload,
                OriginalPacket = packet
            };
        }

        /// <summary>
        /// Строит ответный IPv4+UDP пакет:
        ///   src/dst IP и порты меняем местами,
        ///   payload = ответ от внешнего сервера.
        /// 
        /// Это пакет, который мы пошлём назад в TUN на клиенте.
        /// </summary>
        public static byte[] BuildUdpResponsePacket(ParsedUdp request, byte[] responsePayload)
        {
            // IPv4 header 20 байт (без опций)
            const int ipHeaderLen = 20;
            const int udpHeaderLen = 8;

            ushort totalLen = (ushort)(ipHeaderLen + udpHeaderLen + responsePayload.Length);
            ushort udpLen = (ushort)(udpHeaderLen + responsePayload.Length);

            var buffer = new byte[totalLen];

            // ---------------- IPv4 header ----------------
            // Version(4) + IHL(4)
            buffer[0] = 0x45; // Version=4, IHL=5 (20 байт)
            buffer[1] = 0x00; // DSCP/ECN

            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(2, 2), totalLen); // Total length

            // Identification
            ushort identification = (ushort)Random.Shared.Next(0, ushort.MaxValue);
            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(4, 2), identification);

            // Flags/Fragment offset
            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(6, 2), 0x4000); // Don't Fragment

            // TTL
            buffer[8] = request.Ttl != 0 ? request.Ttl : (byte)64;

            // Protocol
            buffer[9] = ProtocolUdp;

            // Src IP = оригинальный Dest IP
            request.DstIp.GetAddressBytes().CopyTo(buffer, 12);

            // Dst IP = оригинальный Src IP
            request.SrcIp.GetAddressBytes().CopyTo(buffer, 16);

            // IP checksum (пока 0)
            buffer[10] = 0;
            buffer[11] = 0;
            ushort ipChecksum = ComputeIpChecksum(buffer.AsSpan(0, ipHeaderLen));
            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(10, 2), ipChecksum);

            // ---------------- UDP header ----------------
            int udpOffset = ipHeaderLen;

            // SrcPort = оригинальный DstPort
            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(udpOffset, 2), request.DstPort);
            // DstPort = оригинальный SrcPort
            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(udpOffset + 2, 2), request.SrcPort);

            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(udpOffset + 4, 2), udpLen);

            // UDP checksum = 0 (допустимо, многие стеки принимают)
            buffer[udpOffset + 6] = 0;
            buffer[udpOffset + 7] = 0;

            // Payload
            responsePayload.CopyTo(buffer.AsSpan(udpOffset + 8));

            // Можно посчитать UDP checksum по псевдо-заголовку, но для MVP оставим 0.

            return buffer;
        }

        private static ushort ComputeIpChecksum(ReadOnlySpan<byte> header)
        {
            uint sum = 0;

            for (int i = 0; i < header.Length; i += 2)
            {
                ushort word = (i + 1 < header.Length)
                    ? BinaryPrimitives.ReadUInt16BigEndian(header.Slice(i, 2))
                    : (ushort)(header[i] << 8);

                sum += word;
                if ((sum & 0xFFFF0000) != 0)
                {
                    sum = (sum & 0xFFFF) + (sum >> 16);
                }
            }

            while ((sum >> 16) != 0)
            {
                sum = (sum & 0xFFFF) + (sum >> 16);
            }

            return (ushort)~sum;
        }
    }
}
