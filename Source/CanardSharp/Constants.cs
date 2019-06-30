using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp
{
    class Constants
    {
        public const int TRANSFER_TIMEOUT_USEC = 2000000;

        public const int TRANSFER_ID_BIT_LEN = 5;
        public const int ANON_MSG_DATA_TYPE_ID_BIT_LEN = 2;

        /// This will be changed when the support for CAN FD is added
        public const uint CANARD_CAN_FRAME_MAX_DATA_LEN = 8U;

        /// Node ID values. Refer to the specification for more info.
        public const byte CANARD_BROADCAST_NODE_ID = 0;
        public const int CANARD_MIN_NODE_ID = 1;
        public const int CANARD_MAX_NODE_ID = 127;

        /// Refer to canardCleanupStaleTransfers() for details.
        public const uint CANARD_RECOMMENDED_STALE_TRANSFER_CLEANUP_INTERVAL_USEC = 1000000U;
    }
}
