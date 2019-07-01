using CanardSharp;
using CanardSharp.Dsdl.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp
{
    public class TransferReceivedArgs : EventArgs
    {
        public IUavcanType Type { get; set; }
        public Lazy<object> Content { get; set; }
        public byte[] ContentBytes { get; set; }
        public DateTime ReceivedTime { get; set; }
        public CanardPriority Priority { get; set; }
        public int SourceNodeId { get; set; }
        public byte TransferId { get; set; }
    }
}
