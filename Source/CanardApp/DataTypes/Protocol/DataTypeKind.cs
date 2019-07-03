using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CanardApp.DataTypes.Protocol
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
