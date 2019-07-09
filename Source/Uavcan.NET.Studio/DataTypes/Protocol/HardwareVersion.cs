using System.Runtime.Serialization;

namespace Uavcan.NET.Studio.DataTypes.Protocol
{
    /// <summary>
    /// Generic hardware version information.
    /// </summary>
    [DataContract(Name = "HardwareVersion", Namespace = "uavcan.protocol")]
    sealed class HardwareVersion
    {
        [DataMember(Name = "major")]
        public byte Major { get; set; }

        [DataMember(Name = "minor")]
        public byte Minor { get; set; }

        /// <summary>
        /// Unique ID is a 128 bit long sequence that is globally unique for each node.
        /// </summary>
        /// <remarks>
        /// All zeros is not a valid UID.
        /// If filled with zeros, assume that the value is undefined.
        /// </remarks>
        [DataMember(Name = "unique_id")]
        public byte[] UniqueId { get; set; }

        /// <summary>
        /// Certificate of authenticity (COA) of the hardware, 255 bytes max.
        /// </summary>
        [DataMember(Name = "certificate_of_authenticity")]
        public byte[] CertificateOfAuthenticity { get; set; }
    }
}
