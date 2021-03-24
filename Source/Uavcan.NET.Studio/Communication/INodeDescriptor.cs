using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Uavcan.NET.Studio.DataTypes.Protocol;

namespace Uavcan.NET.Studio.Communication
{
    public interface INodeDescriptor : INotifyPropertyChanged
    {
        NodeHandle Handle { get; }

        DateTimeOffset Registered { get; }
        DateTimeOffset Updated { get; }

        INodeInfo Info { get; }
        INodeStatus Status { get; }
    }
}
