using FlowPhantom.Application.DTO;
using FlowPhantom.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Application.Services
{
    /// <summary>
    /// Сервис отправки сегментов на сервер.
    /// Оборачивает ISender, но делает логику централизованной.
    /// </summary>
    public class SendService
    {
        private readonly ISender _sender;

        public SendService(ISender sender)
        {
            _sender = sender;
        }

        public async Task SendAsync(MaskedSegmentDto segment, CancellationToken ct)
        {
            await _sender.SendAsync(segment.Data, ct);
            Console.WriteLine($"[SEND] Segment {segment.Index} sent ({segment.Data.Length} bytes)");
        }
    }
}
