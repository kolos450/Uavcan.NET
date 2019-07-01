using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp
{
    public readonly struct TransferDescriptor : IEquatable<TransferDescriptor>
    {
        public TransferDescriptor(uint dataTypeId, CanardTransferType transferType, uint srcNodeId, uint dstNodeId)
        {
            Value = (((uint)(dataTypeId)) | (((uint)(transferType)) << 16) |
                (((uint)(srcNodeId)) << 18) | (((uint)(dstNodeId)) << 25));
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
