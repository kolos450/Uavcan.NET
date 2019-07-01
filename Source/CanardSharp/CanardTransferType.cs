using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp
{
    /// <summary>
    /// Transfer types are defined by the UAVCAN specification.
    /// </summary>
    public enum CanardTransferType
    {
        CanardTransferTypeResponse = 0,
        CanardTransferTypeRequest = 1,
        CanardTransferTypeBroadcast = 2
    }
}
