using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp
{
    public class CanardRxTransfer
    {
        /// <summary>
        /// Timestamp at which the first frame of this transfer was received.
        /// </summary>
        public ulong TimestampUsec;

        public uint DataTypeId; // 0 to 255 for services, 0 to 65535 for messages.
        public CanardTransferType TransferType;
        public byte TransferId; // 0 to 31
        public CanardPriority Priority; // 0 to 31
        public byte SourceNodeId; // 1 to 127, or 0 if the source is anonymous.

        public byte[] Payload { get; set; }
    };
}
