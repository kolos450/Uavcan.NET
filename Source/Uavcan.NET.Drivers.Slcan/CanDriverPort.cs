using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET.Drivers.Slcan
{
    sealed class CanDriverPort : ICanDriverPort
    {
        public CanDriverPort(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            DisplayName = name;
        }

        public string DisplayName { get; }

        public bool Equals(ICanDriverPort other)
        {
            return Equals(this, other as CanDriverPort);
        }

        public override bool Equals(object other)
        {
            return Equals(this, other as CanDriverPort);
        }

        static bool Equals(CanDriverPort a, CanDriverPort b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (a is null || b is null)
                return false;
            return string.Equals(a.DisplayName, b.DisplayName, StringComparison.Ordinal);
        }

        public override int GetHashCode() => DisplayName.GetHashCode();

        public override string ToString() => DisplayName;

        public ICanDriver Open(int bitrate)
        {
            var usbTin = new UsbTin();
            usbTin.Connect(DisplayName);
            usbTin.OpenCanChannel(bitrate, UsbTinOpenMode.Active);
            return usbTin;
        }
    }
}
