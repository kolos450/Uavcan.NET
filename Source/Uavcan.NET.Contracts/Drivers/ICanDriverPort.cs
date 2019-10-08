using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET.Drivers
{
    public interface ICanDriverPort : IEquatable<ICanDriverPort>
    {
        string DisplayName { get; }
        ICanDriver Open(int bitrate);
    }
}
