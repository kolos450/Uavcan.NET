using System.Runtime.Serialization;

namespace CanardApp.DataTypes.Protocol.Param
{
    /// <summary>
    /// Single parameter value.
    /// </summary>
    /// <remarks>
    /// This is a union, which means that this structure can contain either one of the fields below.
    /// The structure is prefixed with tag - a selector value that indicates which particular field is encoded.
    /// </remarks>
    [DataContract(Name = "Value", Namespace = "uavcan.protocol.param")]
    sealed class Value
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
    }
}