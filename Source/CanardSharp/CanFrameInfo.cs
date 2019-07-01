using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp
{
    public readonly struct CanFrameInfo
    {
        public CanFrameInfo(CanFrame frame)
        {
            var tailByte = frame.Data[frame.DataOffset + frame.DataLength - 1];

            TransferId = (byte)(tailByte & 0x1F);
            IsStartOfTransfer = ((tailByte >> 7) & 1) == 1;
            IsEndOfTransfer = ((tailByte >> 6) & 1) == 1;
            ToggleBit = ((tailByte >> 5) & 1) == 1;
        }

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
