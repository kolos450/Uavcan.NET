using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET.Drivers.Slcan
{
    [Export(typeof(ICanPortProvider))]
    sealed class CanPortProvider : ICanPortProvider
    {
        public string Name => "Serial line CAN interface driver";

        public IEnumerable<ICanPort> GetDriverPorts()
        {
            return SerialPort.GetPortNames()
                .Select(x => new CanPort(x));
        }
    }
}
