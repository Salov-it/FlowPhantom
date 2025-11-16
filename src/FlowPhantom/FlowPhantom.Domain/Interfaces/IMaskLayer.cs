using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Domain.Interfaces
{
    /// <summary>
    /// IMaskLayer — маскирующий слой.
    ///
    /// Он принимает обычный "сырой" чанк VPN-трафика
    /// и превращает его в "псевдо-видеосегмент".
    ///
    /// Что делает настоящий маскирующий слой:
    /// --------------------------------------
    /// ✔ добавляет HTTP-заголовки, как у VK CDN
    /// ✔ формирует Range-запросы (bytes=100000-200000)
    /// ✔ добавляет задержки, характерные для видеоплеера
    /// ✔ формирует QUIC/HTTP3 кадры
    /// ✔ подставляет User-Agent VKPlay / VKClient
    /// ✔ упаковывает полезную нагрузку в видео-структуру
    ///
    /// Domain задаёт интерфейс, а реализация (VkVideoMaskLayer)
    /// будет в Infrastructure.
    /// </summary>
    public interface IMaskLayer
    {
        /// <summary>
        /// Маскирует данный чанк в "видеосегмент".
        /// 
        /// chunk — входные данные VPN
        /// ct — токен отмены
        ///
        /// Возвращает массив байт, который выглядит как видеосегмент.
        /// </summary>
        Task<byte[]> ApplyMaskAsync(byte[] chunk, CancellationToken ct = default);
    }
}
