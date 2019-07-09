using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET
{
    public readonly struct CanFramesProcessingResult
    {
        public CanFramesProcessingResult(UavcanRxTransfer transfer, IReadOnlyList<CanFrame> sourceFrames)
        {
            Transfer = transfer;
            SourceFrames = sourceFrames;
        }

        public readonly UavcanRxTransfer Transfer;
        public readonly IReadOnlyList<CanFrame> SourceFrames;
    }
}
