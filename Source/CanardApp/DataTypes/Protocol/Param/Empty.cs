using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CanardApp.DataTypes.Protocol.Param
{
    /// <summary>
    /// Ex nihilo nihil fit.
    /// </summary>
    [DataContract(Name = "Empty", Namespace = "uavcan.protocol.param")]
    sealed class Empty
    {
    }
}
