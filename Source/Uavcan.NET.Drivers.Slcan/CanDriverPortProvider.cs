using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET.Drivers.Slcan
{
    [Export(typeof(ICanDriverPortProvider))]
    sealed class CanDriverPortProvider : ICanDriverPortProvider
    {
        public IEnumerable<ICanDriverPort> GetDriverPorts()
        {
            return SerialPort.GetPortNames()
                .Select(x => new CanDriverPort(x));
        }
    }
}
