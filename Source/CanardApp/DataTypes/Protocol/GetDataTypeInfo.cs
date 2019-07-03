using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CanardApp.DataTypes.Protocol
{
    /// <summary>
    /// Get the implementation details of a given data type.
    /// </summary>
    /// <remarks>
    /// Request is interpreted as follows:
    ///  - If the field <see cref="Name"/> is empty, the fields <see cref="Kind"/> and <see cref="Id"/> will be used to identify the data type.
    ///  - If the field '<see cref="Name"/> is non-empty, it will be used to identify the data type; the
    ///    fields <see cref="Kind"/> and <see cref="Id"/> will be ignored.
    /// </remarks>
    [DataContract(Name = "GetDataTypeInfo.Request", Namespace = "uavcan.protocol")]
    sealed class GetDataTypeInfo_Request
    {
        /// <summary>
        /// Ignored if <see cref="Name"/> is non-empty.
        /// </summary>
        [DataMember(Name = "id")]

        public ushort Id { get; set; }

        /// <summary>
        /// Ignored if <see cref="Name"/> is non-empty.
        /// </summary>
        [DataMember(Name = "kind")]
        public DataTypeKind Kind { get; set; }

        /// <summary>
        /// Full data type name, e.g. "uavcan.protocol.GetDataTypeInfo"
        /// </summary>
        [DataMember(Name = "name")]
        public byte[] Name { get; set; }
    }

    [DataContract(Name = "GetDataTypeInfo.Response", Namespace = "uavcan.protocol")]
    sealed class GetDataTypeInfo_Response
    {
        /// <summary>
        /// Data type signature; valid only if the data type is known, <see cref="DataTypeFlags.Known"/>.
        /// </summary>
        [DataMember(Name = "signature")]
        public ulong Signature { get; set; }

        /// <summary>
        /// Valid only if the data type is known, <see cref="DataTypeFlags.Known"/>.
        /// </summary>
        [DataMember(Name = "id")]
        public ushort Id { get; set; }

        /// <summary>
        /// Ditto
        /// </summary>
        [DataMember(Name = "kind")]
        public DataTypeKind Kind { get; set; }

        public enum DataTypeFlags : byte
        {
            /// <summary>
            /// This data type is defined.
            /// </summary>
            Known = 1,

            /// <summary>
            /// Subscribed to messages of this type.
            /// </summary>
            Subscribed = 2,

            /// <summary>
            /// Publishing messages of this type.
            /// </summary>
            Publishing = 4,

            /// <summary>
            /// Providing service of this type.
            /// </summary>
            Serving = 8
        }

        /// <summary>
        /// Full data type name.
        /// </summary>
        [DataMember(Name = "flags")]
        public DataTypeFlags Flags { get; set; }


        [DataMember(Name = "name")]
        public byte[] Name { get; set; }
    }
}
