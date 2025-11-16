using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Client.Transport
{
    /// <summary>
    /// Разрезает payload на чанки (MTU-friendly) и собирает обратно.
    /// </summary>
    public static class Chunker
    {
        public static IEnumerable<byte[]> Split(byte[] data, int chunkSize)
        {
            for (int i = 0; i < data.Length; i += chunkSize)
            {
                int size = Math.Min(chunkSize, data.Length - i);
                yield return data.AsSpan(i, size).ToArray();
            }
        }

        public static byte[] Combine(List<byte[]> chunks)
        {
            int size = chunks.Sum(c => c.Length);
            byte[] result = new byte[size];

            int offset = 0;
            foreach (var chunk in chunks)
            {
                Buffer.BlockCopy(chunk, 0, result, offset, chunk.Length);
                offset += chunk.Length;
            }

            return result;
        }
    }
}
