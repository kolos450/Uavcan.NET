using CanardSharp;
using CanardSharp.Dsdl.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CanardApp.Tools.BusMonitor.Presentation
{
    sealed class FrameModel
    {
        public FrameDirection Direction { get; set; }
        public DateTime Time { get; set; }
        public CanId CanId { get; set; }
        public byte[] Data { get; set; }
        public int SourceNodeId { get; set; }
        public int DestinationNodeId { get; set; }
        public object DataType { get; set; }
    }
}
