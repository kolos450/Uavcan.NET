using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Uavcan.NET.Drivers
{
    public interface ICanPort : IEquatable<ICanPort>
    {
        string DisplayName { get; }
        Task<ICanInterface> OpenAsync(int bitrate, CancellationToken cancellationToken);
    }
}
