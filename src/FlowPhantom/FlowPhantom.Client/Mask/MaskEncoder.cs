using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Client.Mask
{
  
    /// MaskEncoder — маскирует исходящие данные под “сегменты видео VK”.
    ///
    /// Идея:
    ///  - каждый наш пакет выглядит как кусок видеопотока;
    ///  - у него есть псевдо-заголовок: профиль, качество, id сегмента;
    ///  - общий размер сегмента подгоняется под диапазон 8–64 КБ (как обычные чанки);
    ///  - хвост забивается рандомным padding'ом.
    ///
    /// Структура маскированного блока (big-endian):
    ///   [CodecProfile:1]        — тип кодека/профиля (псевдо)
    ///   [QualityCode:1]         — “качество” (360p/480p/720p/... псевдо)
    ///   [SegmentId:4]           — инкрементирующий id сегмента
    ///   [PayloadLength:2]       — длина полезной нагрузки
    ///   [PaddingLength:2]       — длина паддинга
    ///   [Payload:PayloadLength] — наши реальные данные
    ///   [Padding:PaddingLength] — случайный шум
    ///
    /// Всё, кроме Payload, можно считать “видеомусором” для DPI.
    /// </summary>
    public static class MaskEncoder
    {
        // Диапазон “типичных” размеров видео-сегмента (в байтах)
        // Можно подстраивать под реальные паттерны.
        private const int MinSegmentSize = 8 * 1024;   // 8 KB
        private const int MaxSegmentSize = 64 * 1024;  // 64 KB

        // Глобальный счётчик сегментов
        private static uint _segmentId;

        private static readonly byte[] QualityCodes =
        {
        0x24, // условно 360p
        0x32, // 480p
        0x40, // 720p
        0x50  // 1080p
    };

        private static readonly byte[] CodecProfiles =
        {
        0x01, // H264 main
        0x02, // H264 high
        0x03, // VP9
        0x04  // AV1
    };

        private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

        public static byte[] Encode(byte[] payload)
        {
            // Выбираем псевдо “профиль кодека”
            byte codecProfile = CodecProfiles[RandomNumberGenerator.GetInt32(0, CodecProfiles.Length)];

            // Выбираем псевдо “качество видео”
            byte qualityCode = QualityCodes[RandomNumberGenerator.GetInt32(0, QualityCodes.Length)];

            // Увеличиваем глобальный id сегмента
            uint segmentId = unchecked(++_segmentId);

            // Длина полезной нагрузки
            ushort payloadLength = (ushort)payload.Length;

            // Размер будущего сегмента: в диапазоне Min..Max
            int targetSegmentSize = RandomNumberGenerator.GetInt32(MinSegmentSize, MaxSegmentSize + 1);

            // Размер заголовка (1+1+4+2+2 = 10 байт)
            const int headerSize = 10;

            // Сколько места остаётся под padding:
            int availableForPadding = targetSegmentSize - headerSize - payloadLength;
            if (availableForPadding < 0)
            {
                // Если полезная нагрузка уже больше, чем хотели — делаем минимальный padding
                availableForPadding = RandomNumberGenerator.GetInt32(0, 32);
            }

            ushort paddingLength = (ushort)availableForPadding;

            // Готовим паддинг
            byte[] padding = new byte[paddingLength];
            if (paddingLength > 0)
                Rng.GetBytes(padding);

            // Итоговый буфер
            byte[] result = new byte[headerSize + payloadLength + paddingLength];

            int offset = 0;

            // CodecProfile
            result[offset++] = codecProfile;

            // QualityCode
            result[offset++] = qualityCode;

            // SegmentId (4 байта, big-endian)
            BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(offset, 4), segmentId);
            offset += 4;

            // PayloadLength (2 байта)
            BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(offset, 2), payloadLength);
            offset += 2;

            // PaddingLength (2 байта)
            BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(offset, 2), paddingLength);
            offset += 2;

            // Payload
            Buffer.BlockCopy(payload, 0, result, offset, payloadLength);
            offset += payloadLength;

            // Padding
            if (paddingLength > 0)
                Buffer.BlockCopy(padding, 0, result, offset, paddingLength);

            return result;
        }
    }
}
