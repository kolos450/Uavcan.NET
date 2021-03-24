using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Uavcan.NET.Studio.DataTypes.Protocol;

namespace Uavcan.NET.Studio.Communication
{
    public interface INodeStatus : INotifyPropertyChanged
    {
        NodeMode Mode { get; }
        byte SubMode { get; }
        NodeHealth Health { get; }
        TimeSpan Uptime { get; }
        ushort VendorSpecificStatusCode { get; }
    }
}
