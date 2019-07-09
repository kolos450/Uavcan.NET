using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET
{
    public class UavcanConstants
    {
        public const int TransferTimeoutUsec = 2000000;

        public const int TransferIdBitLen = 5;
        public const int AnonymousMessageDataTypeIdBitLen = 2;

        /// This will be changed when the support for CAN FD is added
        public const uint CanFrameMaxDataLen = 8U;

        /// Node ID values. Refer to the specification for more info.
        public const byte BroadcastNodeId = 0;
        public const int MinNodeId = 1;
        public const int MaxNodeId = 127;
    }
}
