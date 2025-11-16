using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Server.Reassembly
{
    /// <summary>
    /// Сборщик (Reassembler) сегментов обратно в единый поток байт.
    ///
    /// Логика:
    /// -------
    /// - Каждый сегмент имеет segmentId: 0, 1, 2, ...
    /// - Сегменты могут приходить не по порядку (0, 2, 1, 3, ...)
    /// - Мы складываем их во внутреннее хранилище.
    /// - Как только собрана непрерывная последовательность от _nextExpectedId,
    ///   мы собираем её в один buffer и возвращаем.
    ///
    /// Пример:
    ///   Add(0, A) → вернёт A
    ///   Add(1, B) → вернёт B
    ///   Add(3, D) → вернёт null (ждём 2)
    ///   Add(2, C) → вернёт C + D (если nextExpectedId был 2)
    ///
    /// Таким образом можно собирать поток по мере прихода фреймов.
    /// </summary>
    public class Reassembler
    {
        // Хранилище сегментов: segmentId → payload
        private readonly SortedDictionary<int, byte[]> _segments = new();

        // Следующий ожидаемый ID сегмента (по умолчанию 0)
        private int _nextExpectedId = 0;

        private readonly object _lock = new();

        /// <summary>
        /// Добавить сегмент.
        ///
        /// Возвращает:
        /// - null, если пока не удалось собрать непрерывную последовательность
        /// - byte[], если удалось собрать один или несколько сегментов подряд
        ///   начиная с _nextExpectedId.
        /// </summary>
        public byte[]? AddSegment(int segmentId, byte[] payload)
        {
            lock (_lock)
            {
                // Если этот segmentId уже есть — перезаписывать не будем (можно логировать)
                if (!_segments.ContainsKey(segmentId))
                {
                    _segments[segmentId] = payload;
                }

                // Если этот сегмент меньше, чем уже "отданные" — игнорируем
                if (segmentId < _nextExpectedId)
                {
                    return null;
                }

                // Проверяем, можем ли сейчас собрать непрерывную последовательность
                if (!_segments.ContainsKey(_nextExpectedId))
                {
                    // Нет даже следующего ожидаемого сегмента — ждём
                    return null;
                }

                // Соберём всё, что подряд: _nextExpectedId, _nextExpectedId+1, ...
                var collected = new List<byte[]>();
                int currentId = _nextExpectedId;

                while (_segments.TryGetValue(currentId, out var segmentPayload))
                {
                    collected.Add(segmentPayload);
                    _segments.Remove(currentId);
                    currentId++;
                }

                // Обновляем ожидаемый ID
                _nextExpectedId = currentId;

                // Склеиваем все собранные payload в один массив
                int totalLength = collected.Sum(p => p.Length);
                var result = new byte[totalLength];

                int offset = 0;
                foreach (var part in collected)
                {
                    Buffer.BlockCopy(part, 0, result, offset, part.Length);
                    offset += part.Length;
                }

                return result;
            }
        }

        /// <summary>
        /// Сбросить состояние, если нужно начать новый поток.
        /// </summary>
        public void Reset(int startSegmentId = 0)
        {
            lock (_lock)
            {
                _segments.Clear();
                _nextExpectedId = startSegmentId;
            }
        }
    }
}
