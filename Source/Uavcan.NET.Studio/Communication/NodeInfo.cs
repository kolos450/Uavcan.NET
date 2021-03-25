using System;
using System.Collections.Generic;
using System.Text;

namespace Uavcan.NET.Studio.Communication
{
    sealed class NodeInfo : NotifyPropertyChanged, INodeInfo
    {
        string _name;
        public string Name { get => _name; set => SetField(ref _name, value); }
    }
}
