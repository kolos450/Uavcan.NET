using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET.Dsdl.DataTypes
{
    public class UIntDsdlType : PrimitiveDsdlType
    {
        public UIntDsdlType(int bitlen, CastMode castMode) : base(bitlen, castMode)
        {
        }

        protected override bool ValidateBitlen(int bitlen)
        {
            return bitlen >= 2 && bitlen <= 64;
        }

        public override string GetNormalizedMemberDefinition()
        {
            var castMode = _castMode == CastMode.Saturated ? "saturated" : "truncated";
            return $"{castMode} uint{_bitlen}";
        }

        public override bool IsInRange(object value)
        {
            ulong ulongValue;

            switch (value)
            {
                case bool v:
                    ulongValue = v ? 1u : 0u;
                    break;
                case char v:
                    ulongValue = v;
                    break;
                case sbyte v:
                    if (v < 0)
                        return false;
                    ulongValue = (ulong)v;
                    break;
                case byte v:
                    ulongValue = v;
                    break;
                case int v:
                    if (v < 0)
                        return false;
                    ulongValue = (ulong)v;
                    break;
                case uint v:
                    ulongValue = v;
                    break;
                case long v:
                    if (v < 0)
                        return false;
                    ulongValue = (ulong)v;
                    break;
                case ulong v:
                    ulongValue = v;
                    break;
                default:
                    return false;
            }

            return TypeLimits.GetUIntRange(_bitlen).ContainsValue(ulongValue);
        }
    }
}
