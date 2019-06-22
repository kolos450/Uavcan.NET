using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp.Dsdl
{
    class Signature
    {
        const ulong MASK64 = 0xFFFFFFFFFFFFFFFF;
        const ulong POLY = 0x42F0E1EBA9EA3693;
        ulong _crc;

        public Signature()
        {
            _crc = MASK64;
        }

        public Signature(ulong v)
        {
            _crc = (v & MASK64) ^ MASK64;
        }

        public ulong Value => (_crc & MASK64) ^ MASK64;

        public void Add(string value) => Add(value.Select(x => (byte)x).ToArray());

        public void Add(byte[] v)
        {
            foreach (var b in v)
            {
                _crc ^= ((ulong)b << 56) & MASK64;
                for (int i = 0; i < 8; i++)
                {
                    if ((_crc & (1UL << 63)) != 0)
                        _crc = ((_crc << 1) & MASK64) ^ POLY;
                    else
                        _crc <<= 1;
                }
            }
        }

        public static ulong Compute(string input)
        {
            var sig = new Signature();
            sig.Add(input);
            return sig.Value;
        }

        public static byte[] bytes_from_crc64(ulong crc64) =>
            LittleEndianBitConverter.GetBytes(crc64);
    }
}
