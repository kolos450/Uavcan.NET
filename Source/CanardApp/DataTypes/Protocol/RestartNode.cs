using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CanardApp.DataTypes.Protocol
{
    /// <summary>
    /// Restart the node.
    /// </summary>
    /// <remarks>
    /// Some nodes may require restart before the new configuration will be applied.
    /// The request should be rejected if magic_number does not equal <see cref="MagicNumberValue"/>.
    /// </remarks>
    [DataContract(Name = "RestartNode.Request", Namespace = "uavcan.protocol")]
    sealed class RestartNode_Request
    {
        public const ulong MagicNumberValue = 0xACCE551B1E;
        [DataMember(Name = "magic_number")]
        public ulong MagicNumber { get; set; }
    }

    [DataContract(Name = "RestartNode.Response", Namespace = "uavcan.protocol")]
    sealed class RestartNode_Response
    {
        [DataMember(Name = "ok")]
        public bool Ok { get; set; }
    }
}
