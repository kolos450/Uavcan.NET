using System.Threading;
using System.Threading.Tasks;
using Uavcan.NET.IO.Can.Drivers;

namespace Uavcan.NET.Studio.Diagnostics
{
    sealed class DebugCanPort : ICanPort
    {
        public string DisplayName => "Debug";

        public bool Equals(ICanPort other)
        {
            return ReferenceEquals(this, other);
        }

        public Task<ICanInterface> OpenAsync(int bitrate, CancellationToken cancellationToken)
        {
            return Task.FromResult<ICanInterface>(new DebugCanInterface(bitrate));
        }
    }
}