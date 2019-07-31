using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET.Dsdl.DataTypes
{
    public class FloatDsdlType : PrimitiveDsdlType
    {
        public FloatDsdlType(int bitlen, CastMode castMode)
            : base(bitlen, castMode)
        {
            if (!ValidateBitlen(bitlen))
                throw new ArgumentException($"Invalid bit length: {bitlen}.", nameof(bitlen));
        }

        static bool ValidateBitlen(int bitlen)
        {
            return bitlen == 16 || bitlen == 32 || bitlen == 64;
        }

        public override string GetNormalizedMemberDefinition()
        {
            var castMode = _castMode == CastMode.Saturated ? "saturated" : "truncated";
            return $"{castMode} float{_bitlen}";
        }

        public override bool IsInRange(object value)
        {
            double doubleValue;

            switch (value)
            {
                case float v:
                    doubleValue = v;
                    break;
                case double v:
                    doubleValue = v;
                    break;
                default:
                    return false;
            }

            return TypeLimits.GetFloatRange(_bitlen).ContainsValue(doubleValue);
        }
    }
}
