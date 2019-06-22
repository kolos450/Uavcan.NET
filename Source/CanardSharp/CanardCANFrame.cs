using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp
{
    /**
         * This data type holds a standard CAN 2.0B data frame with 29-bit ID.
         */
    public class CanardCANFrame : IComparable<CanardCANFrame>
    {
        /**
         * Refer to the following definitions:
         *  - CANARD_CAN_FRAME_EFF
         *  - CANARD_CAN_FRAME_RTR
         *  - CANARD_CAN_FRAME_ERR
         */
        public CanId Id;
        public byte[] Data;
        public byte DataLength;

        public int CompareTo(CanardCANFrame other)
        {
            if (other == null)
                return -1;

            return Id.CompareTo(other.Id);
        }

        public CanFrameInfo GetFrameInfo()
        {
            var tailByte = Data[DataLength - 1];
            return new CanFrameInfo(
                transferId: (byte)(tailByte & 0x1F),
                isStartOfTransfer: ((tailByte >> 7) & 1) == 1,
                isEndOfTransfer: ((tailByte >> 6) & 1) == 1,
                toggleBit: ((tailByte >> 5) & 1) == 1
            );
        }
    }
}
