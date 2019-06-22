using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp.Dsdl.DataTypes
{
    class BooleanDsdlType : PrimitiveDsdlType
    {
        public BooleanDsdlType(CastMode castMode) : base(1, castMode)
        {
            if (castMode != CastMode.Saturated)
                throw new ArgumentException("Invalid cast mode for boolean.", nameof(CastMode));
        }

        protected override bool ValidateBitlen(int bitlen)
        {
            return bitlen == 1;
        }

        public override string GetNormalizedMemberDefinition()
        {
            var castMode = _castMode == CastMode.Saturated ? "saturated" : "truncated";
            return $"{castMode} bool";
        }

        public override bool IsInRange(object value)
        {
            switch (value)
            {
                case bool _:
                    return true;
                case char v:
                    return v == 0 || v == 1;
                case sbyte v:
                    return v == 0 || v == 1;
                case byte v:
                    return v == 0 || v == 1;
                case int v:
                    return v == 0 || v == 1;
                case uint v:
                    return v == 0 || v == 1;
                case long v:
                    return v == 0 || v == 1;
                case ulong v:
                    return v == 0 || v == 1;
                default:
                    return false;
            }
        }
    }
}
