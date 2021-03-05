using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uavcan.NET.IO.Can;

namespace Uavcan.NET
{
    static class CanFrameExtensions
    {
        public static CanFrameInfo GetFrameInfo(this CanFrame frame)
        {
            var tailByte = frame.Data[frame.DataOffset + frame.DataLength - 1];
            return new CanFrameInfo(
                transferId: (byte)(tailByte & 0x1F),
                isStartOfTransfer: ((tailByte >> 7) & 1) == 1,
                isEndOfTransfer: ((tailByte >> 6) & 1) == 1,
                toggleBit: ((tailByte >> 5) & 1) == 1
            );
        }
    }
}
