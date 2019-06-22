using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp.Dsdl.DataTypes
{
    static class TypeLimits
    {
        public static Range<ulong> GetUIntRange(int bitlen)
        {
            if (bitlen < 1 || bitlen > 64)
                throw new Exception($"Invalid bit length for integer type: {bitlen}.");
            return Range.Create(0U, (ulong)(1UL << bitlen) - 1);
        }

        public static Range<long> GetIntRange(int bitlen)
        {
            var uintRangeMax = GetUIntRange(bitlen).Maximum;
            return Range.Create(-(long)(uintRangeMax / 2) - 1, (long)(uintRangeMax / 2));
        }

        public static Range<double> GetFloatRange(int bitlen)
        {
            double maxValue;
            switch (bitlen)
            {
                case 16:
                    maxValue = 65504.0;
                    break;
                case 32:
                    maxValue = 3.40282346638528859812e+38;
                    break;
                case 64:
                    maxValue = 1.79769313486231570815e+308;
                    break;
                default:
                    throw new Exception($"Invalid bit length for float type: {bitlen}.");
            }

            return Range.Create(-maxValue, maxValue);
        }
    }
}
