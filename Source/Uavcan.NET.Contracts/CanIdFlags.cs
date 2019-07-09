using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET
{
    [Flags]
    public enum CanIdFlags : uint
    {
        /// <summary>
        /// Extended frame format.
        /// </summary>
        EFF = 1U << 31,

        /// <summary>
        /// Remote transmission.
        /// </summary>
        RTR = 1U << 30,

        /// <summary>
        /// Error frame.
        /// </summary>
        ERR = 1U << 29,

        Mask = EFF | RTR | ERR,
    }
}
