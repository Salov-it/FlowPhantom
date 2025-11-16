using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Application.Services
{
    /// <summary>
    /// PipelineService — основная логика FlowPhantom клиента.
    ///
    /// Он делает:
    /// 1. Получает сырой VPN-трафик
    /// 2. Разрезает на чанки (ChunkService)
    /// 3. Маскирует каждый чанк под видео (MaskService)
    /// 4. Отправляет сегмент на сервер (SendService)
    ///
    /// Позже сюда добавим:
    /// - задержки в стиле видеопотока
    /// - переподключение
    /// - адаптивный bitrate-подобный алгоритм
    /// </summary>
    public class PipelineService
    {
        private readonly ChunkService _chunkService;
        private readonly MaskService _maskService;
        private readonly SendService _sendService;

        public PipelineService(
            ChunkService chunkService,
            MaskService maskService,
            SendService sendService)
        {
            _chunkService = chunkService;
            _maskService = maskService;
            _sendService = sendService;
        }

        /// <summary>
        /// Запуск полного пайплайна.
        /// </summary>
        public async Task ProcessAsync(byte[] rawData, CancellationToken ct)
        {
            Console.WriteLine("[PIPELINE] Starting chunking...");
            var chunks = _chunkService.CreateChunks(rawData);

            foreach (var chunk in chunks)
            {
                ct.ThrowIfCancellationRequested();

                // 1) Маскируем чанк
                var masked = await _maskService.MaskAsync(chunk, ct);

                // 2) Отправляем на сервер
                await _sendService.SendAsync(masked, ct);

                // 3) Тут позже добавим видеоподобные задержки
                // await Task.Delay(randomDelay, ct);
            }

            Console.WriteLine("[PIPELINE] Completed.");
        }
    }
}
