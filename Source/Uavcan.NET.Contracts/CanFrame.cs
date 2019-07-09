using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET
{
    /// <summary>
    /// This data type holds a standard CAN 2.0B data frame with 29-bit ID.
    /// </summary>
    public class CanFrame : IComparable<CanFrame>
    {
        public CanFrame(uint id, byte[] data, int dataOffset, int dataLength)
            : this(new CanId(id), data, dataOffset, dataLength)
        { }

        public CanFrame(CanId id, byte[] data, int dataOffset, int dataLength)
        {
            Id = id;
            Data = data;
            DataOffset = dataOffset;
            DataLength = dataLength;
        }

        public readonly CanId Id;
        public readonly byte[] Data;
        public readonly int DataLength;
        public readonly int DataOffset;

        public int CompareTo(CanFrame other)
        {
            if (other == null)
                return -1;

            return Id.CompareTo(other.Id);
        }
    }
}
