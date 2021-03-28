using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Uavcan.NET.IO.Can.Drivers;

namespace Uavcan.NET.Studio.Diagnostics
{
    sealed class DebugCanPort : ICanPort, IEquatable<DebugCanPort>
    {
        public string DisplayName => "Debug";

        public bool Equals(ICanPort other) => Equals(this, other as DebugCanPort);
        public bool Equals(DebugCanPort other) => Equals(this, other);
        public override bool Equals(object obj) => Equals(this, obj as DebugCanPort);
        public override int GetHashCode() => 0;

        static bool Equals(DebugCanPort a, DebugCanPort b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (a is null || b is null)
                return false;
            return true;
        }

        public Task<ICanInterface> OpenAsync(int bitrate, CancellationToken cancellationToken)
        {
            return Task.FromResult<ICanInterface>(new DebugCanInterface(bitrate));
        }
    }
}