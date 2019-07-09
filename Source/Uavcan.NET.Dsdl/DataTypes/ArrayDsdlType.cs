using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET.Dsdl.DataTypes
{
    public class ArrayDsdlType : DsdlType
    {
        DsdlType _elementType;
        ArrayDsdlTypeMode _mode;
        int _maxSize;

        public DsdlType ElementType => _elementType;
        public ArrayDsdlTypeMode Mode => _mode;
        public int MaxSize => _maxSize;

        public ArrayDsdlType(DsdlType valueType, ArrayDsdlTypeMode mode, int maxSize)
        {
            if (maxSize <= 0)
                throw new ArgumentException("Array size must be positive.", nameof(maxSize));

            _elementType = valueType;
            _mode = mode;
            _maxSize = maxSize;
        }

        public override int MaxBitlen
        {
            get
            {
                switch (_mode)
                {
                    case ArrayDsdlTypeMode.Dynamic:
                        return _maxSize * _elementType.MaxBitlen + _maxSize.GetBitLength();
                    case ArrayDsdlTypeMode.Static:
                        return _maxSize * _elementType.MaxBitlen;
                    default:
                        throw new ArgumentException();
                }
            }
        }

        public override int MinBitlen
        {
            get
            {
                switch (_mode)
                {
                    case ArrayDsdlTypeMode.Dynamic:
                        return 0;
                    case ArrayDsdlTypeMode.Static:
                        return _maxSize * _elementType.MinBitlen;
                    default:
                        throw new ArgumentException();
                }
            }
        }

        public override string GetNormalizedMemberDefinition()
        {
            switch (_mode)
            {
                case ArrayDsdlTypeMode.Dynamic:
                    return $"{_elementType}[<={_maxSize}]";
                case ArrayDsdlTypeMode.Static:
                    return $"{_elementType}[{_maxSize}]";
                default:
                    throw new ArgumentException();
            }
        }

        public bool IsStringLike =>
            _mode == ArrayDsdlTypeMode.Dynamic &&
            _elementType is PrimitiveDsdlType &&
            _elementType.MaxBitlen == 8;

        public override ulong? GetDataTypeSignature() => _elementType.GetDataTypeSignature();

        internal void SetElementType(DsdlType type)
        {
            _elementType = type;
        }
    }
}
