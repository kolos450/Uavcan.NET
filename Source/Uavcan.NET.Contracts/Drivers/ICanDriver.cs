using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET.Drivers
{
    public interface ICanDriver : IDisposable
    {
        event EventHandler<CanMessageEventArgs> MessageReceived;
        event EventHandler<CanMessageEventArgs> MessageTransmitted;

        void Send(CanFrame canmsg);
    }
}
