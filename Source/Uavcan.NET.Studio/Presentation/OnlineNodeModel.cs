using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Uavcan.NET.Studio.DataTypes.Protocol;

namespace Uavcan.NET.Studio.Presentation
{
    class OnlineNodeModel : INotifyPropertyChanged
    {
        int _nodeId;
        string _name;
        NodeMode _mode;
        NodeHealth _health;
        TimeSpan _uptime;
        ushort _vssc;

        void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public int NodeId { get => _nodeId; set => SetField(ref _nodeId, value); }
        public string Name { get => _name; set => SetField(ref _name, value); }
        public NodeMode Mode { get => _mode; set => SetField(ref _mode, value); }
        public NodeHealth Health { get => _health; set => SetField(ref _health, value); }
        public TimeSpan Uptime { get => _uptime; set => SetField(ref _uptime, value); }
        public ushort VSSC { get => _vssc; set => SetField(ref _vssc, value); }

        public DateTimeOffset Updated { get; set; }
    }
}
