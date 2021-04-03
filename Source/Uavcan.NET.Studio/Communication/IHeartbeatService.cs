using System;
using Uavcan.NET.Studio.DataTypes.Protocol;

namespace Uavcan.NET.Studio.Communication
{
    public interface IHeartbeatService
    {
        NodeHealth Health { get; set; }
        TimeSpan Interval { get; set; }
        NodeMode Mode { get; set; }
        byte SubMode { get; set; }
        ushort VendorSpecificStatusCode { get; set; }

        void Start();
        void Stop();
    }
}