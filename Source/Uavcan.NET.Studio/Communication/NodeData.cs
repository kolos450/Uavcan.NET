using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Uavcan.NET.Studio.DataTypes.Protocol;

namespace Uavcan.NET.Studio.Communication
{
    sealed class NodeData : NotifyPropertyChanged, INodeStatus
    {
        NodeMode _mode;
        public NodeMode Mode { get => _mode; set => SetField(ref _mode, value); }

        byte _subMode;
        public byte SubMode { get => _subMode; set => SetField(ref _subMode, value); }

        NodeHealth _health;
        public NodeHealth Health { get => _health; set => SetField(ref _health, value); }

        TimeSpan _uptime;
        public TimeSpan Uptime { get => _uptime; set => SetField(ref _uptime, value); }

        ushort _vendorSpecificStatusCode;
        public ushort VendorSpecificStatusCode
        {
            get => _vendorSpecificStatusCode;
            set => SetField(ref _vendorSpecificStatusCode, value);
        }

        public void Update(NodeStatus status)
        {
            Health = status.Health;
            Mode = status.Mode;
            SubMode = status.SubMode;
            Uptime = TimeSpan.FromSeconds(status.UptimeSec);
            VendorSpecificStatusCode = status.VendorSpecificStatusCode;
        }
    }
}
