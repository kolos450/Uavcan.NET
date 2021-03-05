using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET.IO.Can
{
    public readonly struct CanId : IComparable<CanId>
    {
        public const uint CanExtendedIdMask = 0x1FFFFFFFU;
        public const uint CanStandardIdMask = 0x000007FFU;

        public readonly uint Value;

        public CanId(uint value)
        {
            Value = value;
        }

        public CanIdFlags Flags => (CanIdFlags)(Value & (uint)CanIdFlags.Mask);

        public int CompareTo(CanId value)
        {
            uint self = Value;
            uint other = value.Value;

            uint self_clean_id = self & CanExtendedIdMask;
            uint other_clean_id = other & CanExtendedIdMask;

            /*
             * STD vs EXT - if 11 most significant bits are the same, EXT loses.
             */
            bool self_ext = (self & (uint)CanIdFlags.EFF) != 0;
            bool other_ext = (other & (uint)CanIdFlags.EFF) != 0;
            if (self_ext != other_ext)
            {
                uint self_arb11 = self_ext ? self_clean_id >> 18 : self_clean_id;
                uint other_arb11 = other_ext ? other_clean_id >> 18 : other_clean_id;
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

        public override string ToString()
        {
            return Convert.ToString(Value, 16);
        }
    }
}
