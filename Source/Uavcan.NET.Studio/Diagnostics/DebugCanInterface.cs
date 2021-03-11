using System;
using Uavcan.NET.IO.Can;
using Uavcan.NET.IO.Can.Drivers;

namespace Uavcan.NET.Studio.Diagnostics
{
    internal class DebugCanInterface : ICanInterface
    {
        readonly int _bitrate;

        public DebugCanInterface(int bitrate)
        {
            _bitrate = bitrate;
        }

        public event EventHandler<CanMessageEventArgs> MessageReceived;
        public event EventHandler<CanMessageEventArgs> MessageTransmitted;

        public void Dispose() { }

        public void Send(CanFrame canmsg)
        {
            MessageTransmitted?.Invoke(
                this,
                new CanMessageEventArgs
                {
                    Message = canmsg
                });
        }
    }
}