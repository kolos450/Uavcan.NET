using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp.Dsdl.DataTypes
{
    public class ArrayDsdlType : DsdlType
    {
        DsdlType _valueType;
        ArrayDsdlTypeMode _mode;
        int _maxSize;

        public DsdlType ItemType => _valueType;

        public ArrayDsdlType(DsdlType valueType, ArrayDsdlTypeMode mode, int maxSize)
        {
            if (maxSize <= 0)
                throw new ArgumentException("Array size must be positive.", nameof(maxSize));

            _valueType = valueType;
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
                        return _maxSize * _valueType.MaxBitlen + _maxSize.GetBitLength();
                    case ArrayDsdlTypeMode.Static:
                        return _maxSize * _valueType.MaxBitlen;
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
                        return _maxSize * _valueType.MinBitlen;
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
                    return $"{_valueType}[<={_maxSize}]";
                case ArrayDsdlTypeMode.Static:
                    return $"{_valueType}[{_maxSize}]";
                default:
                    throw new ArgumentException();
            }
        }

        public bool IsStringLike =>
            _mode == ArrayDsdlTypeMode.Dynamic &&
            _valueType is PrimitiveDsdlType &&
            _valueType.MaxBitlen == 8;

        public override ulong? GetDataTypeSignature() => _valueType.GetDataTypeSignature();
    }
}
