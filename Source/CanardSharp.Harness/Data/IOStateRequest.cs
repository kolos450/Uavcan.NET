using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp.Harness.Data
{
    [DataContract(Name = "IOState:Request", Namespace = "kplc")]
    sealed class IOStateRequest
    {
        [DataMember(Name = "state")]
        public byte[] State { get; set; }

        [DataMember(Name = "state_inv")]
        public byte[] StateInverted { get; set; }
    }
}
