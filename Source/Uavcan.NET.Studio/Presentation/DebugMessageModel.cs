using System;
using Uavcan.NET.Studio.DataTypes.Protocol.Debug;

namespace Uavcan.NET.Studio.Presentation
{
    sealed class DebugMessageModel
    {
        public int NodeId { get; set; }
        public DateTime Time { get; set; }
        public LogLevel.ValueKind Level { get; set; }
        public string Source { get; set; }
        public string Text { get; set; }
    }
}
