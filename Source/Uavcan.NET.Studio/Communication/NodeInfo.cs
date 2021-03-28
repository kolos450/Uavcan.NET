using System;
using System.Collections.Generic;
using System.Text;
using Uavcan.NET.Studio.DataTypes.Protocol;

namespace Uavcan.NET.Studio.Communication
{
    sealed class NodeInfo : NotifyPropertyChanged, INodeInfo
    {
        string _name;
        public string Name { get => _name; set => SetField(ref _name, value); }

        public void Update(GetNodeInfo_Response data)
        {
            Name = data.Name;
        }
    }
}
