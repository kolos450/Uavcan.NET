using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CanardApp.DataTypes.Protocol
{
    /// <summary>
    /// Generic software version information.
    /// </summary>
    [DataContract(Name = "SoftwareVersion", Namespace = "uavcan.protocol")]
    sealed class SoftwareVersion
    {
        [DataMember(Name = "major")]
        public byte Major { get; set; }

        [DataMember(Name = "minor")]
        public byte Minor { get; set; }

        public enum OptionalFieldFlags : byte
        {
            None = 0,
            VcsCommit = 1,
            ImageCrc = 2
        }

        /// <summary>
        /// This mask indicates which optional fields are set.
        /// </summary>
        [DataMember(Name = "optional_field_flags")]
        public byte OptionalFieldsValue { get; set; }

        /// <summary>
        /// VCS commit hash or revision number, e.g. git short commit hash.
        /// </summary>
        [DataMember(Name = "vcs_commit")]
        public uint VcsCommit { get; set; }

        /// <summary>
        /// The value of an arbitrary hash function applied to the firmware image.
        /// </summary>
        /// <remarks>
        /// This field is used to detect whether the firmware running on the node is EXACTLY THE SAME
        /// as a certain specific revision. This field provides the absolute identity guarantee, unlike
        /// the version fields above, which can be the same for different builds of the firmware.
        /// 
        /// The exact hash function and the methods of its application are implementation defined.
        /// However, implementations are recommended to adhere to the following guidelines,
        /// fully or partially:
        /// 
        ///   - The hash function should be CRC-64-WE, the same that is used for computing DSDL signatures.
        /// 
        ///   - The hash function should be applied to the entire application image padded to 8 bytes.
        /// 
        ///   - If the computed image CRC is stored within the firmware image itself, the value of
        ///     the hash function becomes ill-defined, because it becomes recursively dependent on itself.
        ///     In order to circumvent this issue, while computing or checking the CRC, its value stored
        ///     within the image should be zeroed out.
        /// </remarks>
        [DataMember(Name = "image_crc")]
        public ulong ImageCrc { get; set; }
    }
}
