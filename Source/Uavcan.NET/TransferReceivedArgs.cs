using Uavcan.NET.Dsdl.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET
{
    public class TransferReceivedArgs : EventArgs
    {
        public IUavcanType Type { get; set; }
        public byte[] ContentBytes { get; set; }
        public DateTime ReceivedTime { get; set; }
        public UavcanPriority Priority { get; set; }
        public int SourceNodeId { get; set; }
        public byte TransferId { get; set; }
        public UavcanTransferType TransferType { get; set; }
    }
}
