﻿using System.Runtime.Serialization;

namespace Uavcan.NET.Studio.DataTypes.Protocol.Param
{
    /// <summary>
    /// Numeric-only value.
    /// </summary>
    /// <remarks>
    /// This is a union, which means that this structure can contain either one of the fields below.
    /// The structure is prefixed with tag - a selector value that indicates which particular field is encoded.
    /// </remarks>
    [DataContract(Name = "NumericValue", Namespace = "uavcan.protocol.param")]
    public sealed class NumericValue
    {
        /// <summary>
        /// Empty field, used to represent an undefined value.
        /// </summary>
        [DataMember(Name = "empty")]
        public Empty Empty { get; set; }

        [DataMember(Name = "integer_value")]
        public long? IntegerValue { get; set; }

        [DataMember(Name = "real_value")]
        public float? RealValue { get; set; }
    }
}
