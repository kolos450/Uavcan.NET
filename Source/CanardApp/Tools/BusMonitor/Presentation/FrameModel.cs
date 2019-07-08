using CanardSharp;
using CanardSharp.Dsdl.DataTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CanardApp.Tools.BusMonitor.Presentation
{
    sealed class FrameModel : INotifyPropertyChanged
    {
        object _dataType;

        void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public FrameDirection Direction { get; set; }
        public DateTime Time { get; set; }
        public CanId CanId { get; set; }
        public byte[] Data { get; set; }
        public int? SourceNodeId { get; set; }
        public int? DestinationNodeId { get; set; }
        public object DataType { get => _dataType; set => SetField(ref _dataType, value); }
        public CanardRxTransfer AssociatedTransfer { get; set; }
        public Exception AccociatedFrameProcessorException { get; set; }
    }
}
