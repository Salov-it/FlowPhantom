using FlowPhantom.Client.Mask;
using FlowPhantom.Client.Transport;
using FlowPhantom.Infrastructure.Media;
using FlowPhantom.Infrastructure.Network.Masking;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace FlowPhantom.Client.Services
{
    /// <summary>
    /// Новый TCP FlowClient — полностью совместим с сервером.
    ///
    /// Формат отправки:
    ///   PacketFramer → MediaSegmentCodec → MaskEnvelopeEncoder → TCP length-prefixed
    ///
    /// Формат приёма:
    ///   TCP length-prefixed → MaskEnvelopeDecoder → MediaSegmentCodec.Decode → PacketFramer
    /// </summary>
    public sealed class FlowClient
    {
        private TcpClient? _tcp;
        private NetworkStream? _stream;

        private readonly string _host;
        private readonly int _port;

        private readonly Channel<byte[]> _sendQueue =
            Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

        private bool _running;

        public event Action<byte[]>? OnMessage;

        private const int MaxChunkSize = 1200;
        private const ushort DefaultSessionId = 1;

        public FlowClient(string host, int port)
        {
            _host = host;
            _port = port;
        }

        // ----------------------------------------------------------------
        // Start / Stop
        // ----------------------------------------------------------------
        public void Start()
        {
            if (_running) return;

            _running = true;

            _tcp = new TcpClient();
            _tcp.Connect(_host, _port);

            _stream = _tcp.GetStream();
            Console.WriteLine("[CLIENT] Connected to server");

            _ = Task.Run(SenderLoop);
            _ = Task.Run(ReceiverLoop);
        }

        public void Stop()
        {
            _running = false;
            _stream?.Close();
            _tcp?.Close();
        }

        // ----------------------------------------------------------------
        // Public API
        // ----------------------------------------------------------------
        public async Task SendAsync(byte[] data)
        {
            await _sendQueue.Writer.WriteAsync(data);
        }

        // ----------------------------------------------------------------
        // Sender Loop — отправка кадров
        // ----------------------------------------------------------------
        private async Task SenderLoop()
        {
            if (_stream == null)
                return;

            int streamId = 1;
            int segmentIndex = 0;

            while (_running)
            {
                // Берём IP-пакет / данные
                var data = await _sendQueue.Reader.ReadAsync();

                foreach (var chunk in Chunker.Split(data, MaxChunkSize))
                {
                    // 1) Внутренний фрейм
                    var framed = PacketFramer.Frame(
                        DefaultSessionId,
                        chunk,
                        flags: 0
                    );

                    // 2) Meta
                    var meta = new MediaSegmentMeta(
                        streamId,
                        segmentIndex++,
                        ptsMs: 0,
                        durationMs: 0
                    );

                    // 3) MediaSegment
                    var mediaPayload = MediaSegmentCodec.Encode(meta, framed);

                    // 4) VK Video mask
                    var masked = MaskEnvelopeEncoder.Encode(
                        meta.SegmentIndex,
                        mediaPayload
                    );

                    // 5) Length-prefixed write
                    byte[] lenBuf = new byte[4];
                    BinaryPrimitives.WriteInt32BigEndian(lenBuf, masked.Length);

                    await _stream.WriteAsync(lenBuf);
                    await _stream.WriteAsync(masked);
                    await _stream.FlushAsync();
                }
            }
        }

        // ----------------------------------------------------------------
        // Receiver Loop — приём кадров от сервера
        // ----------------------------------------------------------------
        private async Task ReceiverLoop()
        {
            if (_stream == null) return;
            var stream = _stream;

            try
            {
                while (_running)
                {
                    // 1) Читаем длину
                    byte[] lenBuf = new byte[4];
                    if (!await ReadExactAsync(stream, lenBuf, 0, 4))
                        break;

                    int frameLength = BinaryPrimitives.ReadInt32BigEndian(lenBuf);
                    if (frameLength <= 0)
                        continue;

                    // 2) Читаем сам VK-маскированный фрейм
                    byte[] masked = new byte[frameLength];
                    if (!await ReadExactAsync(stream, masked, 0, frameLength))
                        break;

                    // 3) VK-маска → mediaPayload
                    var maskDecoded = MaskEnvelopeDecoder.DecodeFrame(masked);

                    // 4) mediaPayload → meta + frame
                    var (meta, innerFrame) = MediaSegmentCodec.Decode(maskDecoded.Payload);

                    // 5) frame → IP payload
                    if (!PacketFramer.TryParse(innerFrame, out ushort sessionId, out byte flags, out var payload))
                    {
                        Console.WriteLine("[CLIENT] PacketFramer parse error");
                        continue;
                    }

                    // Notify user
                    OnMessage?.Invoke(payload);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[CLIENT] Receiver error: " + ex.Message);
            }
        }

        // ----------------------------------------------------------------
        // Utility
        // ----------------------------------------------------------------
        private async Task<bool> ReadExactAsync(NetworkStream stream, byte[] buffer, int offset, int count)
        {
            int total = 0;

            while (total < count)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(offset + total, count - total));
                if (read == 0)
                    return false; // disconnected

                total += read;
            }

            return true;
        }
    }
}

