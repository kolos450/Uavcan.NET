using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CanardApp.DataTypes.Protocol.Debug
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
        public byte[] Source { get; set; }
        [DataMember(Name = "text")]
        public byte[] Text { get; set; }
    }
}
