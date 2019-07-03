using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CanardApp.DataTypes.Protocol
{
    /// <summary>
    /// Full node info request.
    /// </summary>
    [DataContract(Name = "GetNodeInfo.Request", Namespace = "uavcan.protocol")]
    sealed class GetNodeInfo_Request
    {
    }

    [DataContract(Name = "GetNodeInfo.Response", Namespace = "uavcan.protocol")]
    sealed class GetNodeInfo_Response
    {
        /// <summary>
        /// Current node status.
        /// </summary>
        [DataMember(Name = "status")]
        public NodeStatus Status { get; set; }

        [DataMember(Name = "software_version")]
        public SoftwareVersion SoftwareVersion { get; set; }

        [DataMember(Name = "hardware_version")]
        public HardwareVersion HardwareVersion { get; set; }

        /// <summary>
        /// Human readable non-empty ASCII node name.
        /// </summary>
        /// <remarks>
        /// Node name shall not be changed while the node is running.
        /// Empty string is not a valid node name.
        /// Allowed characters are: a-z (lowercase ASCII letters) 0-9 (decimal digits) . (dot) - (dash) _ (underscore).
        /// Node name is a reversed internet domain name (like Java packages), e.g. "com.manufacturer.project.product".
        /// </remarks>
        [DataMember(Name = "name")]
        public string Name { get; set; }
    }
}
