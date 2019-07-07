using CanardApp.DataTypes.Protocol.Debug;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardApp.Presentation
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
