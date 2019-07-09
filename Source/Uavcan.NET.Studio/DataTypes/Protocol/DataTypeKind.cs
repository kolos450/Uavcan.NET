using System.Runtime.Serialization;

namespace Uavcan.NET.Studio.DataTypes.Protocol
{
    [DataContract(Name = "DataTypeKind", Namespace = "uavcan.protocol")]
    sealed class DataTypeKind
    {
        public enum ValueKind : byte
        {
            Service = 0,
            Message = 1
        }

        [DataMember(Name = "value")]
        public ValueKind Value { get; set; }
    }
}
