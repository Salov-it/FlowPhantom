using FlowPhantom.Application.DTO;
using FlowPhantom.Domain.Entities;
using FlowPhantom.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Application.Services
{
    /// <summary>
    /// Сервис маскировки чанков.
    /// Принимает чанк и делает из него "видеосегмент".
    /// </summary>
    public class MaskService
    {
        private readonly IMaskLayer _maskLayer;

        public MaskService(IMaskLayer maskLayer)
        {
            _maskLayer = maskLayer;
        }

        public async Task<MaskedSegmentDto> MaskAsync(Chunk chunk, CancellationToken ct)
        {
            var masked = await _maskLayer.ApplyMaskAsync(chunk.Data, ct);
            return new MaskedSegmentDto(chunk.Id.Value, masked);
        }
    }
}
