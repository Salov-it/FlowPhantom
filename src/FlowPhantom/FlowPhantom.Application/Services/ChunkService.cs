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
    /// Сервис для разрезки данных на чанки.
    /// Делегирует работу доменному IChunker.
    /// </summary>
    public class ChunkService
    {
        private readonly IChunker _chunker;

        public ChunkService(IChunker chunker)
        {
            _chunker = chunker;
        }

        /// <summary>
        /// Возвращает список чанков с индексами.
        /// </summary>
        public List<Chunk> CreateChunks(byte[] data)
        {
            var result = new List<Chunk>();
            int index = 0;

            foreach (var chunkBytes in _chunker.Chunkify(data))
            {
                var chunk = new Chunk(new(index), chunkBytes);
                result.Add(chunk);
                index++;
            }

            return result;
        }
    }
}
