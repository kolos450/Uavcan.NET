using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Uavcan.NET.IO.Can.Drivers.Slcan
{
    sealed class CanPort : ICanPort
    {
        public CanPort(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            DisplayName = name;
        }

        public string DisplayName { get; }

        public bool Equals(CanPort other) => Equals(this, other);
        public bool Equals(ICanPort other) => Equals(this, other as CanPort);
        public override bool Equals(object other) => Equals(this, other as CanPort);

        static bool Equals(CanPort a, CanPort b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (a is null || b is null)
                return false;
            return string.Equals(a.DisplayName, b.DisplayName, StringComparison.Ordinal);
        }

        public override int GetHashCode() => DisplayName.GetHashCode();

        public override string ToString() => DisplayName;

        public async Task<ICanInterface> OpenAsync(int bitrate, CancellationToken cancellationToken)
        {
            var usbTin = new UsbTin();
            await usbTin.ConnectAsync(DisplayName, bitrate, UsbTinOpenMode.Active, cancellationToken)
                .ConfigureAwait(false);
            return usbTin;
        }
    }
}
