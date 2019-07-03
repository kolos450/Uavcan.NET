using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CanardApp.DataTypes
{
    [DataContract(Name = "Timestamp", Namespace = "uavcan")]
    sealed class Timestamp
    {
        public const ulong Unknown = 0;

        [DataMember(Name = "usec")]
        public ulong USec { get; set; }
    }
}
