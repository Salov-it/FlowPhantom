
using FlowPhantom.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Infrastructure.Network
{
    /// <summary>
    /// Отправитель, который использует HTTP/3 поверх QUIC.
    ///
    /// Зачем HTTP/3/QUIC:
    /// ------------------
    /// - Современные видеосервисы (YouTube, VK Video) активно
    ///   используют HTTP/3 поверх протокола QUIC.
    /// - DPI часто смотрит на версию протокола и паттерн запросов.
    /// - Если мы шлём трафик как HTTP/3, это гораздо ближе к реальному
    ///   видеостримингу, чем обычный HTTP/1.1.
    ///
    /// Внутри:
    /// - HttpClient с DefaultRequestVersion = HTTP/3.0
    /// - BaseAddress указывает на FlowPhantom.Server
    /// - Каждый сегмент отправляется POST /segment
    /// </summary>
    public class Http3QuicSender : ISender, IDisposable
    {
        private readonly HttpClient _client;

        /// <summary>
        /// baseAddress — адрес FlowPhantom.Server, например:
        ///   "https://your-flowphantom-server.com"
        /// или
        ///   "https://127.0.0.1:5001"
        /// </summary>
        public Http3QuicSender(string baseAddress)
        {
            // ⚙ Настраиваем низкоуровневый сокетный хендлер
            var handler = new SocketsHttpHandler
            {
                // Мы не хотим автоматических сжатий, чтобы меньше выделяться
                AutomaticDecompression = DecompressionMethods.None,

                // Можно включить переиспользование соединений
                EnableMultipleHttp2Connections = true // положительно влияет и на HTTP/3
            };

            // ⚙ HttpClient, настроенный на HTTP/3
            _client = new HttpClient(handler)
            {
                BaseAddress = new Uri(baseAddress),

                // Говорим клиенту: "по умолчанию используй HTTP/3"
                DefaultRequestVersion = HttpVersion.Version30,
                DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
            };

            // Можно настроить заголовки по умолчанию (User-Agent, Accept и т.п.)
            _client.DefaultRequestHeaders.UserAgent.ParseAdd("VKClient/7.33 (Android 13)");
            _client.DefaultRequestHeaders.Accept.ParseAdd("*/*");
        }

        /// <summary>
        /// Отправляет один замаскированный сегмент на сервер.
        /// maskedSegment — это уже результат работы MaskLayer:
        /// [FAKE HTTP HEADER][BODY(=наш VPN-трафик)].
        /// </summary>
        public async Task SendAsync(byte[] maskedSegment, CancellationToken ct = default)
        {
            // Оборачиваем байтовый массив в HTTP-контент
            using var content = new ByteArrayContent(maskedSegment);

            // Тип контента — бинарный
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            // Создаём HTTP/3 запрос
            using var request = new HttpRequestMessage(HttpMethod.Post, "/segment")
            {
                Content = content,

                // Явно указываем, что хотим HTTP/3 для этого запроса
                Version = HttpVersion.Version30,
                VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
            };

            // Отправляем запрос, ожидаем только заголовки ответа (нам тело не важно)
            using var response = await _client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                ct
            );

            // Если сервер вернул ошибку — бросаем exception
            response.EnsureSuccessStatusCode();
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
