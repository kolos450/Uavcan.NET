using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET.Dsdl.DataTypes
{
    public class IntDsdlType : PrimitiveDsdlType
    {
        public IntDsdlType(int bitlen, CastMode castMode) : base(bitlen, castMode)
        {
            if (!ValidateBitlen(bitlen))
                throw new ArgumentException($"Invalid bit length: {bitlen}.", nameof(bitlen));
            if (castMode != CastMode.Saturated)
                throw new ArgumentException("Invalid cast mode for signed integer.", nameof(CastMode));
        }

        static bool ValidateBitlen(int bitlen)
        {
            return bitlen >= 2 && bitlen <= 64;
        }

        public override string GetNormalizedMemberDefinition()
        {
            var castMode = _castMode == CastMode.Saturated ? "saturated" : "truncated";
            return $"{castMode} int{_bitlen}";
        }

        public override bool IsInRange(object value)
        {
            long longValue;

            switch (value)
            {
                case bool v:
                    longValue = v ? 1 : 0;
                    break;
                case char v:
                    longValue = v;
                    break;
                case sbyte v:
                    longValue = v;
                    break;
                case byte v:
                    longValue = v;
                    break;
                case int v:
                    longValue = v;
                    break;
                case uint v:
                    longValue = v;
                    break;
                case long v:
                    longValue = v;
                    break;
                case ulong v:
                    if (v > long.MaxValue)
                        return false;
                    longValue = (long)v;
                    break;
                default:
                    return false;
            }

            return TypeLimits.GetIntRange(_bitlen).ContainsValue(longValue);
        }
    }
}
