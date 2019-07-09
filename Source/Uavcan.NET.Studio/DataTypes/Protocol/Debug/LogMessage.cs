using System.Runtime.Serialization;

namespace Uavcan.NET.Studio.DataTypes.Protocol.Debug
{
    /// <summary>
    /// Generic log message.
    /// </summary>
    [DataContract(Name = "LogMessage", Namespace = "uavcan.protocol.debug")]
    sealed class LogMessage
    {
        [DataMember(Name = "level")]
        public LogLevel Level { get; set; }

        [DataMember(Name = "source")]
        public string Source { get; set; }

        [DataMember(Name = "text")]
        public string Text { get; set; }
    }
}
