using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Application.DTO
{
    /// <summary>
    /// DTO, который содержит результат маскировки.
    /// Этот объект будет передаваться в SendService.
    /// </summary>
    public class MaskedSegmentDto
    {
        public int Index { get; }
        public byte[] Data { get; }

        public MaskedSegmentDto(int index, byte[] data)
        {
            Index = index;
            Data = data;
        }
    }
}
