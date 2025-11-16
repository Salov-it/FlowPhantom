using FlowPhantom.Application.Interfaces;
using FlowPhantom.Domain.Interfaces;
using FlowPhantom.Infrastructure.Network;
using FlowPhantom.Infrastructure.Network.Masking.Chunking;
using FlowPhantom.Infrastructure.Network.Masking.VkVideoMask;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Infrastructure
{
    /// <summary>
    /// Регистрация инфраструктурных реализаций:
    /// - чанкер
    /// - маскирующий слой
    /// - sender (HTTP/3 + QUIC)
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddFlowPhantomInfrastructure(this IServiceCollection services,string serverBaseAddress)
        {
            // Чанкер: реалистичное видеоподобное разбиение
            services.AddSingleton<IChunker, VkVideoLikeChunker>();

            // Маскирующий слой: VK-видео-заголовки
            services.AddSingleton<IMaskLayer, FakeVkMaskLayer>();

            // Отправитель: HTTP/3 (QUIC)
            services.AddSingleton<ISender>(_ =>
                new Http3QuicSender(serverBaseAddress));

            return services;
        }
    }
}
