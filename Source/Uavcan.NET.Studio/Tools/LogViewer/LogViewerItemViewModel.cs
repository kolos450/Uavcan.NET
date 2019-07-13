using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uavcan.NET.Studio.DataTypes.Protocol;
using Uavcan.NET.Studio.DataTypes.Protocol.Debug;

namespace Uavcan.NET.Studio.Tools.LogViewer
{
    sealed class LogViewerItemViewModel : ReactiveObject
    {
        public int NodeId { get; set; }
        public DateTime Time { get; set; }
        public LogLevel.ValueKind Level { get; set; }
        public string Source { get; set; }
        public string Text { get; set; }

        internal LogViewerItemViewModel(int nodeId, DateTime time, LogMessage value)
        {
            NodeId = nodeId;
            Time = time;
            Level = value.Level.Value;
            Source = value.Source;
            Text = value.Text;
        }

        internal LogViewerItemViewModel(int nodeId, DateTime time, Panic value)
        {
            NodeId = nodeId;
            Time = time;
            Text = value.ReasonText;
        }
    }
}
