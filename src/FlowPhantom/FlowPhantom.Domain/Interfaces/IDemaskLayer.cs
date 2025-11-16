using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Domain.Interfaces
{
    /// <summary>
    /// IDemaskLayer — слой для СЕРВЕРНОЙ стороны FlowPhantom.
    ///
    /// Он получает "замаскированные" данные, пришедшие от клиента
    /// (как будто это видеосегменты), и должен извлечь из них 
    /// настоящие VPN-данные.
    ///
    /// На стороне сервера:
    /// ----------------------------------
    /// ✔ Сервер принимает HTTP/3/QUIC запрос
    /// ✔ Получает "видеосегмент" (payload + маска)
    /// ✔ Демаскирует, удаляя фейковые заголовки/структуру
    /// ✔ Получает исходный VPN-трафик
    /// ✔ Отправляет его в WireGuard/Xray backend
    ///
    /// Domain только определяет интерфейс.
    /// Реализация (VkVideoDemaskLayer) будет в Server/Infrastucture.
    /// </summary>
    public interface IDemaskLayer
    {
        /// <summary>
        /// Снимает маскировку.
        ///
        /// На вход приходит поток (Stream) ― это тело HTTP/3 запроса,
        /// которое выглядит как "видео".
        ///
        /// Метод должен вернуть исходные чистые VPN-байты.
        /// </summary>
        Task<byte[]> RemoveMaskAsync(Stream maskedStream, CancellationToken ct = default);
    }
}
