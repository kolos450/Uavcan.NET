using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp
{
    public readonly struct CanIdInfo
    {
        public CanIdInfo(CanId id)
        {
            var value = id.Value;
            SourceId = (byte)((value >> 0) & 0x7FU);
            IsServiceNotMessage = ((value >> 7) & 0x1) == 1;
            IsRequestNotResponse = ((value >> 15) & 0x1) == 1;
            DestinationId = (byte)((value >> 8) & 0x7FU);
            Priority = (CanardPriority)((value >> 24) & 0x1FU);
            MessageType = (value >> 8) & 0xFFFFU;
            ServiceType = (value >> 16) & 0xFFU;
        }

        public readonly byte SourceId;
        public readonly bool IsServiceNotMessage;
        public readonly bool IsRequestNotResponse;
        public readonly byte DestinationId;
        public readonly CanardPriority Priority;
        public readonly uint MessageType;
        public readonly uint ServiceType;

        public CanardTransferType TransferType
        {
            get
            {
                if (!IsServiceNotMessage)
                {
                    return CanardTransferType.CanardTransferTypeBroadcast;
                }
                else if (IsRequestNotResponse)
                {
                    return CanardTransferType.CanardTransferTypeRequest;
                }
                else
                {
                    return CanardTransferType.CanardTransferTypeResponse;
                }
            }
        }

        public uint DataType
        {
            get
            {
                if (TransferType == CanardTransferType.CanardTransferTypeBroadcast)
                {
                    var dtid = MessageType;
                    if (SourceId == CanardConstants.BroadcastNodeId)
                    {
                        dtid &= (1U << CanardConstants.AnonymousMessageDataTypeIdBitLen) - 1U;
                    }
                    return dtid;
                }
                else
                {
                    return ServiceType;
                }
            }
        }
    }
}
