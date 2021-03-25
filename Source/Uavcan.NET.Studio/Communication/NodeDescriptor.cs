using System;
using System.ComponentModel;

namespace Uavcan.NET.Studio.Communication
{
    sealed class NodeDescriptor : NotifyPropertyChanged, INodeDescriptor
    {
        readonly NodeMonitor _monitor;

        public NodeDescriptor(NodeMonitor monitor, NodeHandle handle)
        {
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            Handle = handle;
        }

        public NodeHandle Handle { get; }

        DateTimeOffset _registered;
        public DateTimeOffset Registered { get => _registered; set => SetField(ref _registered, value); }

        DateTimeOffset _updated;
        public DateTimeOffset Updated { get => _updated; set => SetField(ref _updated, value); }

        INodeInfo _info;
        public INodeInfo Info { get => _info; set => SetField(ref _info, value); }

        INodeStatus _status;
        public INodeStatus Status { get => _status; set => SetField(ref _status, value); }
    }
}