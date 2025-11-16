using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Application.Interfaces
{
    /// <summary>
    /// Интерфейс отправки сегментов на сервер FlowPhantom.Server.
    ///
    /// В нашем случае реализация будет использовать HTTP/3 (QUIC),
    /// чтобы трафик выглядел как современный видеостриминг.
    /// </summary>
    public interface ISender
    {
        /// <summary>
        /// Отправляет один замаскированный сегмент на сервер.
        /// maskedSegment — это уже "видеоподобный" байтовый массив.
        /// </summary>
        Task SendAsync(byte[] maskedSegment, CancellationToken ct = default);
    }
}
