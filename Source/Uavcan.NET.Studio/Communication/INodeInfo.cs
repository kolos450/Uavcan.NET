using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Uavcan.NET.Studio.Communication
{
    public interface INodeInfo : INotifyPropertyChanged
    {
        string Name { get; }
    }
}
