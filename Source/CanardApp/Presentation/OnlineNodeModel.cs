using CanardApp.DataTypes.Protocol;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CanardApp.Presentation
{
    class OnlineNodeModel : INotifyPropertyChanged
    {
        int _nodeId;
        string _name;
        NodeStatus.ModeKind _mode;
        NodeStatus.HealthKind _health;
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
        public NodeStatus.ModeKind Mode { get => _mode; set => SetField(ref _mode, value); }
        public NodeStatus.HealthKind Health { get => _health; set => SetField(ref _health, value); }
        public TimeSpan Uptime { get => _uptime; set => SetField(ref _uptime, value); }
        public ushort VSSC { get => _vssc; set => SetField(ref _vssc, value); }

        public DateTimeOffset Updated { get; set; }
    }
}
