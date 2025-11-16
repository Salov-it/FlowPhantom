using System.Net;
using System.Net.Sockets;
using System.Buffers.Binary;
using FlowPhantom.Infrastructure.Media;
using FlowPhantom.Infrastructure.Network.Masking;
using FlowPhantom.Server.Reassembly;
using FlowPhantom.Server.Network;
using FlowPhantom.Infrastructure.Common.Protocol;
using System.Threading;

static class FlowPhantomServer
{
    private static int _serverSegmentCounter = 0;

    static async Task Main()
    {
        Console.WriteLine("FlowPhantom VPN Server started");
        Console.WriteLine("Listening on TCP port 5001...");

        var reassembler = new Reassembler();
        var nat = new UdpNatRouter();

        var listener = new TcpListener(IPAddress.Any, 5001);
        listener.Start();

        while (true)
        {
            Console.WriteLine("[SERVER] Waiting for client...");
            var client = await listener.AcceptTcpClientAsync();
            Console.WriteLine("[SERVER] Client connected.");

            _ = Task.Run(() => HandleClientAsync(client, reassembler, nat));
        }
    }

    private static async Task HandleClientAsync(
        TcpClient client,
        Reassembler reassembler,
        UdpNatRouter nat)
    {
        using var tcp = client;
        using var stream = tcp.GetStream();

        try
        {
            while (true)
            {
                // =========================
                // 1) ЧИТАЕМ LENGTH
                // =========================
                var lenBuf = new byte[4];
                if (!await ReadExactAsync(stream, lenBuf, 0, 4))
                {
                    Console.WriteLine("[SERVER] Client disconnected (len).");
                    break;
                }

                int frameLen = BinaryPrimitives.ReadInt32BigEndian(lenBuf);
                if (frameLen <= 0 || frameLen > 10_000_000)
                {
                    Console.WriteLine("[SERVER] Invalid frame length.");
                    break;
                }

                // =========================
                // 2) ЧИТАЕМ МАСКИРОВАННЫЙ ФРЕЙМ
                // =========================
                var rawFrame = new byte[frameLen];
                if (!await ReadExactAsync(stream, rawFrame, 0, frameLen))
                {
                    Console.WriteLine("[SERVER] Client disconnected (frame).");
                    break;
                }

                Console.WriteLine($"[SERVER] Frame received: {frameLen} bytes");

                // =========================
                // 3) MASK → mediaPayload
                // =========================
                var maskFrame = MaskEnvelopeDecoder.DecodeFrame(rawFrame);

                // =========================
                // 4) MediaSegment → meta + raw
                // =========================
                var (meta, rawData) = MediaSegmentCodec.Decode(maskFrame.Payload);

                Console.WriteLine(
                    $"[SERVER] Media: stream={meta.StreamId}, seg={meta.SegmentIndex}, raw={rawData.Length}"
                );

                // =========================
                // 5) Reassembler → цельный блок
                // =========================
                var assembled = reassembler.AddSegment(meta.SegmentIndex, rawData);

                if (assembled == null)
                    continue;

                Console.WriteLine($"[SERVER] Assembled block: {assembled.Length} bytes");

                // =========================
                // 6) PacketFramer → IP-пакет
                // =========================
                if (!PacketFramer.TryParse(assembled, out ushort sessionId, out byte flags, out var ipPacket))
                {
                    Console.WriteLine("[SERVER] PacketFramer failed");
                    continue;
                }

                // =========================
                // 7) NAT UDP
                // =========================
                var natResponse = await nat.ForwardAsync(ipPacket);

                if (natResponse == null)
                {
                    Console.WriteLine("[SERVER] NAT produced no response");
                    continue;
                }

                Console.WriteLine($"[SERVER] NAT response: {natResponse.Length} bytes");

                // =========================
                // 8) ОБРАТНЫЙ ОТВЕТ → 
                // Frame → Media → Mask → TCP
                // =========================

                // 8.1) IP → внутренний Frame
                var responseFrame = PacketFramer.Frame(
                    sessionId,
                    natResponse,
                    flags: 0
                );

                // 8.2) MediaSegmentMeta для ответа
                var responseMeta = new MediaSegmentMeta(
                    meta.StreamId,         // тот же поток
                    meta.SegmentIndex,     // можно оставить тот же
                    meta.PtsMs,            // можно не менять
                    meta.DurationMs
                );

                // 8.3) Encode MediaSegment
                var responseMedia = MediaSegmentCodec.Encode(
                    responseMeta,
                    responseFrame
                );

                // 8.4) Генерируем segmentId для VK-mask
                int serverSegmentId = Interlocked.Increment(ref _serverSegmentCounter);

                // 8.5) MaskEncoder
                var maskedResponse = MaskEnvelopeEncoder.Encode(
                    serverSegmentId,
                    responseMedia
                );

                // 8.6) TCP length-prefixed
                var outLen = new byte[4];
                BinaryPrimitives.WriteInt32BigEndian(outLen, maskedResponse.Length);

                await stream.WriteAsync(outLen);
                await stream.WriteAsync(maskedResponse);
                await stream.FlushAsync();

                Console.WriteLine($"[SERVER] Response sent: {maskedResponse.Length} bytes");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[SERVER] ERROR: " + ex);
        }

        Console.WriteLine("[SERVER] Client disconnected.");
    }

    // =========================
    // Utility
    // =========================
    private static async Task<bool> ReadExactAsync(NetworkStream stream, byte[] buffer, int offset, int count)
    {
        int readTotal = 0;
        while (readTotal < count)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(offset + readTotal, count - readTotal));
            if (read == 0)
                return false;

            readTotal += read;
        }
        return true;
    }
}
