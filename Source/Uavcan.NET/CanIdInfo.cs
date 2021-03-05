using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uavcan.NET.IO.Can;

namespace Uavcan.NET
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
            Priority = (UavcanPriority)((value >> 24) & 0x1FU);
            MessageType = (value >> 8) & 0xFFFFU;
            ServiceType = (value >> 16) & 0xFFU;
        }

        public readonly byte SourceId;
        public readonly bool IsServiceNotMessage;
        public readonly bool IsRequestNotResponse;
        public readonly byte DestinationId;
        public readonly UavcanPriority Priority;
        public readonly uint MessageType;
        public readonly uint ServiceType;

        public UavcanTransferType TransferType
        {
            get
            {
                if (!IsServiceNotMessage)
                {
                    return UavcanTransferType.Broadcast;
                }
                else if (IsRequestNotResponse)
                {
                    return UavcanTransferType.Request;
                }
                else
                {
                    return UavcanTransferType.Response;
                }
            }
        }

        public uint DataType
        {
            get
            {
                if (TransferType == UavcanTransferType.Broadcast)
                {
                    var dtid = MessageType;
                    if (SourceId == UavcanConstants.BroadcastNodeId)
                    {
                        dtid &= (1U << UavcanConstants.AnonymousMessageDataTypeIdBitLen) - 1U;
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
