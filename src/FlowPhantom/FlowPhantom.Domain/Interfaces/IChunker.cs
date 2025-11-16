using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Domain.Interfaces
{
    /// <summary>
    /// IChunker — отвечает за разрезку исходного VPN-трафика
    /// на небольшие куски (чанки).
    ///
    /// Зачем это нужно?
    /// ----------------
    /// Видеосервисы (VK, YouTube, TikTok) загружают видео 
    /// НЕ одним большим потоком, а кусочками (сегментами),
    /// обычно 100KB — 1MB.
    ///
    /// Чтобы маскировка была реалистичной, VPN-трафик нужно
    /// сначала порезать на такие же сегменты, а уже потом
    /// маскировать под "видео".
    ///
    /// Domain-слой определяет интерфейс, а реализация (SimpleChunker,
    /// VkVideoChunker) будет находиться в Infrastructure.
    /// </summary>
    public interface IChunker
    {
        /// <summary>
        /// Разрезает исходный байтовый массив на последовательность чанков.
        /// Каждый чанк — это отдельный кусок данных, который далее 
        /// будет маскироваться под видеосегмент.
        ///
        /// Возвращает последовательность байтовых массивов.
        /// </summary>
        IEnumerable<byte[]> Chunkify(byte[] data);
    }
}
