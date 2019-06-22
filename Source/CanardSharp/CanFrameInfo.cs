using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp
{
    public readonly struct CanFrameInfo
    {
        public CanFrameInfo(byte transferId, bool isStartOfTransfer, bool isEndOfTransfer, bool toggleBit)
        {
            TransferId = transferId;
            IsStartOfTransfer = isStartOfTransfer;
            IsEndOfTransfer = isEndOfTransfer;
            ToggleBit = toggleBit;
        }

        public readonly byte TransferId;
        public readonly bool IsStartOfTransfer;
        public readonly bool IsEndOfTransfer;
        public readonly bool ToggleBit;

    }
}
