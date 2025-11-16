using FlowPhantom.Infrastructure.Network.Masking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Server.Demask
{
    /// <summary>
    /// DemaskLayer для нашего собственного mask-протокола.
    ///
    /// Он принимает бинарный "фрейм", проверяет структуру,
    /// извлекает полезный payload и отдаёт его дальше.
    ///
    /// Слой НЕ привязан к реальным сервисам, 
    /// используется только для локального тестирования
    /// пайплайна Chunk → Mask → HTTP/3 → Server.
    /// </summary>
    public class MaskProtocolDemaskLayer
    {
        /// <summary>
        /// Извлекает полезную нагрузку из маскирующего фрейма:
        /// MAGIC | TYPE | SEGID | PAYLOAD_LEN | PADDING_LEN | PAYLOAD | PADDING
        /// </summary>
        public byte[] ExtractPayload(byte[] rawFrame)
        {
            try
            {
                // Декодирует стандартный mask frame
                var payload = MaskEnvelopeDecoder.Decode(rawFrame);

                return payload;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[DEMASK] Ошибка декодирования: " + ex.Message);
                throw;
            }
        }
    }
}
