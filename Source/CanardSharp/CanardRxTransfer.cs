using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp
{
    /**
     * This structure represents a received transfer for the application.
     * An instance of it is passed to the application via callback when the library receives a new transfer.
     * Pointers to the structure and all its fields are invalidated after the callback returns.
     */
    public class CanardRxTransfer
    {
        /**
         * Timestamp at which the first frame of this transfer was received.
         */
        public ulong TimestampUsec;

        /**
         * These fields identify the transfer for the application.
         */
        public uint DataTypeId;                  ///< 0 to 255 for services, 0 to 65535 for messages
        public byte TransferType;                  ///< See CanardTransferType
        public byte TransferId;                    ///< 0 to 31
        public byte Priority;                       ///< 0 to 31
        public byte SourceNodeId;                 ///< 1 to 127, or 0 if the source is anonymous

        public byte[] Payload { get; set; }
    };
}
