using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET
{
    /// <summary>
    /// Types of service transfers. These are not applicable to message transfers.
    /// </summary>
    public enum UavcanRequestResponse
    {
        Response = 0,
        Request = 1
    }
}
