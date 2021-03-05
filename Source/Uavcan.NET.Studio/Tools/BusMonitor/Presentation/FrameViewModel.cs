using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Uavcan.NET.IO.Can;

namespace Uavcan.NET.Studio.Tools.BusMonitor.Presentation
{
    sealed class FrameViewModel : ReactiveObject
    {
        object _dataType;

        public FrameDirection Direction { get; set; }
        public DateTime Time { get; set; }
        public CanId CanId { get; set; }
        public byte[] Data { get; set; }
        public int? SourceNodeId { get; set; }
        public int? DestinationNodeId { get; set; }
        public object DataType { get => _dataType; set => this.RaiseAndSetIfChanged(ref _dataType, value); }
        public UavcanRxTransfer AssociatedTransfer { get; set; }
        public Exception AccociatedFrameProcessorException { get; set; }
    }
}
