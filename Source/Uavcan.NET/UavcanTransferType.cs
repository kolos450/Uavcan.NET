using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET
{
    /// <summary>
    /// Transfer types are defined by the UAVCAN specification.
    /// </summary>
    public enum UavcanTransferType
    {
        Response = 0,
        Request = 1,
        Broadcast = 2
    }
}
