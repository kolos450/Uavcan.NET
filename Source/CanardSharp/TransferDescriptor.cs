using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp
{
    public readonly struct TransferDescriptor : IEquatable<TransferDescriptor>
    {
        public TransferDescriptor(uint data_type_id, CanardTransferType transfer_type, uint src_node_id, uint dst_node_id)
        {
            Value = (((uint)(data_type_id)) | (((uint)(transfer_type)) << 16) |
                (((uint)(src_node_id)) << 18) | (((uint)(dst_node_id)) << 25));
        }

        public readonly uint Value;

        public override bool Equals(object obj)
        {
            return obj is TransferDescriptor descriptor && Equals(descriptor);
        }

        public bool Equals(TransferDescriptor other)
        {
            return Value == other.Value;
        }

        public override int GetHashCode()
        {
            return -1937169414 + Value.GetHashCode();
        }

        public static bool operator ==(TransferDescriptor left, TransferDescriptor right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TransferDescriptor left, TransferDescriptor right)
        {
            return !(left == right);
        }
    }
}
