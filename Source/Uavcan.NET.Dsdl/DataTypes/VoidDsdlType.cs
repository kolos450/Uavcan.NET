
using System;

namespace Uavcan.NET.Dsdl.DataTypes
{
    public class VoidDsdlType : DsdlType
    {
        int _bitlen;

        public VoidDsdlType(int bitlen)
        {
            _bitlen = bitlen;
        }

        public override int MaxBitlen => _bitlen;
        public override int MinBitlen => _bitlen;
        public override string GetNormalizedMemberDefinition() => $"void{_bitlen}";

        public override ulong? GetDataTypeSignature() => null;
    }
}
