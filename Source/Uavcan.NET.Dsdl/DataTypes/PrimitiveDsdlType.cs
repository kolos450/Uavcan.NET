﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET.Dsdl.DataTypes
{
    public abstract class PrimitiveDsdlType : DsdlType
    {
        protected readonly int _bitlen;
        protected readonly CastMode _castMode;

        public CastMode CastMode => _castMode;

        protected PrimitiveDsdlType(int bitlen, CastMode castMode)
        {
            _bitlen = bitlen;
            _castMode = castMode;
        }

        public override int MaxBitlen => _bitlen;

        public override int MinBitlen => _bitlen;

        public abstract bool IsInRange(object value);

        public override ulong? GetDataTypeSignature() => null;
    }
}
