using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET
{
    public enum UavcanPriority : byte
    {
        Highest = 0,
        High = 8,
        Medium = 16,
        Low = 24,
        Lowest = 31,
    }
}
