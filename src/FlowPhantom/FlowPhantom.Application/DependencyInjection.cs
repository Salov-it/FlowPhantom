using FlowPhantom.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Application
{
    /// <summary>
    /// Расширения для регистрации сервисов Application слоя.
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddFlowPhantomApplication(this IServiceCollection services)
        {
            // Application-слой
            services.AddTransient<ChunkService>();
            services.AddTransient<MaskService>();
            services.AddTransient<SendService>();
            services.AddTransient<PipelineService>();

            return services;
        }
    }
}
