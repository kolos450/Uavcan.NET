using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp
{
    public readonly struct CanId : IComparable<CanId>
    {
        const uint CANARD_CAN_EXT_ID_MASK = 0x1FFFFFFFU;
        const uint CANARD_CAN_STD_ID_MASK = 0x000007FFU;

        public readonly uint Value;

        public CanId(uint value)
        {
            Value = value;
        }

        public byte SourceId => (byte)((Value >> 0) & 0x7FU);
        bool IsServiceNotMessage => ((Value >> 7) & 0x1) == 1;
        bool IsRequestNotResponse => ((Value >> 15) & 0x1) == 1;
        public byte DestinationId => (byte)((Value >> 8) & 0x7FU);
        public CanardPriority Priority => (CanardPriority)((Value >> 24) & 0x1FU);
        public uint MessageType => (Value >> 8) & 0xFFFFU;
        public uint ServiceType => (Value >> 16) & 0xFFU;
        public CanIdFlags Flags => (CanIdFlags)(Value & (uint)CanIdFlags.Mask);

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
                    if (SourceId == Constants.CANARD_BROADCAST_NODE_ID)
                    {
                        dtid &= (1U << Constants.ANON_MSG_DATA_TYPE_ID_BIT_LEN) - 1U;
                    }
                    return dtid;
                }
                else
                {
                    return ServiceType;
                }
            }
        }

        /**
         * Returns true if priority of self is higher than other.
         */
        public int CompareTo(CanId value)
        {
            uint self = Value;
            uint other = value.Value;

            uint self_clean_id = self & CANARD_CAN_EXT_ID_MASK;
            uint other_clean_id = other & CANARD_CAN_EXT_ID_MASK;

            /*
             * STD vs EXT - if 11 most significant bits are the same, EXT loses.
             */
            bool self_ext = (self & (uint)CanIdFlags.EFF) != 0;
            bool other_ext = (other & (uint)CanIdFlags.EFF) != 0;
            if (self_ext != other_ext)
            {
                uint self_arb11 = self_ext ? (self_clean_id >> 18) : self_clean_id;
                uint other_arb11 = other_ext ? (other_clean_id >> 18) : other_clean_id;
                if (self_arb11 != other_arb11)
                {
                    return self_arb11.CompareTo(other_arb11);
                }
                else
                {
                    return other_ext ? 1 : -1;
                }
            }

            /*
             * RTR vs Data frame - if frame identifiers and frame types are the same, RTR loses.
             */
            bool self_rtr = (self & (uint)CanIdFlags.RTR) != 0;
            bool other_rtr = (other & (uint)CanIdFlags.RTR) != 0;
            if (self_clean_id == other_clean_id && self_rtr != other_rtr)
            {
                return other_rtr ? 1 : -1;
            }

            /*
             * Plain ID arbitration - greater value loses.
             */
            return self_clean_id.CompareTo(other_clean_id);
        }
    }
}
