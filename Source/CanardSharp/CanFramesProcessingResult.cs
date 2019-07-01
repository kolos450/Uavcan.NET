using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp
{
    public readonly struct CanFramesProcessingResult
    {
        public CanFramesProcessingResult(CanardRxTransfer transfer, IEnumerable<CanFrame> sourceFrames)
        {
            Transfer = transfer;
            SourceFrames = sourceFrames;
        }

        public readonly CanardRxTransfer Transfer;
        public readonly IEnumerable<CanFrame> SourceFrames;
    }
}
