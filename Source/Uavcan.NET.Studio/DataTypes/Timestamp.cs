using System.Runtime.Serialization;

namespace Uavcan.NET.Studio.DataTypes
{
    [DataContract(Name = "Timestamp", Namespace = "uavcan")]
    sealed class Timestamp
    {
        public const ulong Unknown = 0;

        [DataMember(Name = "usec")]
        public ulong USec { get; set; }
    }
}
