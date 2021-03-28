using System;
using System.ComponentModel;

namespace Uavcan.NET.Studio.Communication
{
    sealed class NodeDescriptor : NotifyPropertyChanged, INodeDescriptor
    {
        readonly NodeMonitor _monitor;

        public NodeDescriptor(NodeMonitor monitor, NodeHandle handle, DateTime registered)
        {
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            Handle = handle;
            Registered = registered;
            _updated = registered;
            _info = new NodeInfo();
            _status = new NodeData();
        }

        public NodeHandle Handle { get; }

        public DateTimeOffset Registered { get; }

        DateTimeOffset _updated;
        public DateTimeOffset Updated { get => _updated; set => SetField(ref _updated, value); }

        INodeInfo _info;
        public INodeInfo Info
        {
            get => _info;
            set
            {
                if (_info is not null)
                    _info.PropertyChanged -= PropertyChanged;

                SetField(ref _info, value);

                if (value is not null)
                    value.PropertyChanged += PropertyChanged;

                void PropertyChanged(object sender, PropertyChangedEventArgs e) =>
                    OnPropertyChanged(nameof(Info));
            }
        }

        INodeStatus _status;
        public INodeStatus Status
        {
            get => _status;
            set
            {
                if (_info is not null)
                    _info.PropertyChanged -= PropertyChanged;

                SetField(ref _status, value);

                if (value is not null)
                    value.PropertyChanged += PropertyChanged;

                void PropertyChanged(object sender, PropertyChangedEventArgs e) =>
                    OnPropertyChanged(nameof(Status));
            }
        }
    }
}