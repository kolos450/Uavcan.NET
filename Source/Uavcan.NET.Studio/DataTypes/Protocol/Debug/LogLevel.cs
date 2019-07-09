using System.Runtime.Serialization;

namespace Uavcan.NET.Studio.DataTypes.Protocol.Debug
{
    /// <summary>
    /// Log message severity
    /// </summary>
    [DataContract(Name = "LogLevel", Namespace = "uavcan.protocol.debug")]
    sealed class LogLevel
    {
        public enum ValueKind : byte
        {
            Debug = 0,
            Info = 1,
            Warning = 2,
            Error = 3,
        }

        [DataMember(Name = "value")]
        public ValueKind Value { get; set; }
    }
}
