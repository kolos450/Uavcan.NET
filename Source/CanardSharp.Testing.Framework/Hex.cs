using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp.Testing.Framework
{
    public static class Hex
    {
        public static byte[] Decode(string hex)
        {
            if (hex.Length % 2 == 1)
                throw new InvalidOperationException("String length should be even.");

            var arr = new byte[hex.Length >> 1];
            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }

            return arr;
        }

        static int GetHexVal(char hex)
        {
            return hex - (hex < 58 ? 48 : (hex < 97 ? 55 : 87));
        }

        public static string Encode(byte[] bytes)
        {
            var hex = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }
    }
}
