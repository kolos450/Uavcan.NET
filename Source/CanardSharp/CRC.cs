using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp
{
    static class CRC
    {
        public const ushort InitialValue = (ushort)0xFFFFU;

        public static ushort AddByte(ushort crc_val, byte input)
        {
            crc_val ^= (ushort)(input << 8);
            for (byte j = 0; j < 8; j++)
            {
                if ((crc_val & 0x8000U) > 0)
                {
                    crc_val = (ushort)((ushort)(crc_val << 1) ^ 0x1021U);
                }
                else
                {
                    crc_val = (ushort)(crc_val << 1);
                }
            }
            return crc_val;
        }

        public static ushort AddSignature(ushort crc_val, ulong data_type_signature)
        {
            for (ushort shift_val = 0; shift_val < 64; shift_val = (ushort)(shift_val + 8U))
            {
                crc_val = AddByte(crc_val, (byte)(data_type_signature >> shift_val));
            }
            return crc_val;
        }

        public static ushort Add(ushort crc_val, byte[] buffer, int offset, int len)
        {
            while (len-- > 0)
            {
                crc_val = AddByte(crc_val, buffer[offset++]);
            }
            return crc_val;
        }
    }
}
