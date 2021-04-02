using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Uavcan.NET.Studio.DataTypes.Protocol.Param
{
    /// <summary>
    /// Single parameter value.
    /// </summary>
    /// <remarks>
    /// This is a union, which means that this structure can contain either one of the fields below.
    /// The structure is prefixed with tag - a selector value that indicates which particular field is encoded.
    /// </remarks>
    [DataContract(Name = "Value", Namespace = "uavcan.protocol.param")]
    public sealed class Value : IEquatable<Value>
    {
        /// <summary>
        /// Empty field, used to represent an undefined value.
        /// </summary>
        [DataMember(Name = "empty")]
        public Empty Empty { get; set; }

        [DataMember(Name = "integer_value")]
        public long? IntegerValue { get; set; }

        /// <summary>
        /// 32-bit type is used to simplify implementation on low-end systems.
        /// </summary>
        [DataMember(Name = "real_value")]
        public float? RealValue { get; set; }

        /// <summary>
        /// 8-bit value is used for alignment reasons.
        /// </summary>
        [DataMember(Name = "boolean_value")]
        public byte? BooleanValue { get; set; }

        /// <summary>
        /// Length prefix is exactly one byte long, which ensures proper alignment of payload.
        /// </summary>
        [DataMember(Name = "string_value")]
        public byte[] StringValue { get; set; }

        public override bool Equals(object obj) =>
            Equals(this, obj as Value);

        public bool Equals(Value other) =>
            Equals(this, other);

        public override int GetHashCode() =>
            HashCode.Combine(IntegerValue, RealValue, BooleanValue, StringValue);

        enum UnionTag
        {
            Empty,
            Integer,
            Real,
            Boolean,
            String
        }

        UnionTag GetTag()
        {
            int counter = 0;
            if (Empty is not null)
                counter++;
            if (IntegerValue is not null)
                counter++;
            if (RealValue is not null)
                counter++;
            if (BooleanValue is not null)
                counter++;
            if (StringValue is not null)
                counter++;
            if (counter != 0)
                throw new InvalidOperationException("Union type must contain exactly 1 non-null value.");

            if (Empty is not null)
                return UnionTag.Empty;
            else if (IntegerValue is not null)
                return UnionTag.Integer;
            else if (RealValue is not null)
                return UnionTag.Real;
            else if (BooleanValue is not null)
                return UnionTag.Boolean;
            else
                return UnionTag.String;
        }

        static bool Equals(Value left, Value right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left is null || right is null)
                return false;

            var leftTag = left.GetTag();
            var rightTag = right.GetTag();
            if (leftTag != rightTag)
                return false;

            switch (leftTag)
            {
                case UnionTag.Empty:
                    return true;

                case UnionTag.Integer:
                    return left.IntegerValue == right.IntegerValue;

                case UnionTag.Real:
                    return left.RealValue == right.RealValue;

                case UnionTag.Boolean:
                    return left.BooleanValue == right.BooleanValue;

                case UnionTag.String:
                    var leftValue = left.StringValue;
                    var rightValue = right.StringValue;
                    if (leftValue.Length != rightValue.Length)
                    {
                        return false;
                    }

                    for (int i = 0; i < leftValue.Length; i++)
                    {
                        if (leftValue[i] != rightValue[i])
                        {
                            return false;
                        }
                    }
                    return true;

                default:
                    throw new InvalidOperationException();
            }
        }
    }
}