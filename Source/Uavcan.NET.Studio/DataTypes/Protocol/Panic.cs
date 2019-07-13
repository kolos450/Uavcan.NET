using System.Runtime.Serialization;

namespace Uavcan.NET.Studio.DataTypes.Protocol
{
    /// <summary>
    /// This message may be published periodically to inform network participants that the system has encountered
    /// an unrecoverable fault and is not capable of further operation.
    /// </summary>
    /// <remarks>
    /// Nodes that are expected to react to this message should wait for at least <see cref="MinMessages"/> subsequent messages
    /// with any reason text from any sender published with the interval no higher than <see cref="MaxIntervalMs"/> before
    /// undertaking any emergency actions.
    /// </remarks>
    [DataContract(Name = "Panic", Namespace = "uavcan.protocol")]
    sealed class Panic
    {
        public const byte MinMessages = 3;
        public const ushort MaxIntervalMs = 500;

        [DataMember(Name = "reason_text")]
        public string ReasonText { get; set; }
    }
}
