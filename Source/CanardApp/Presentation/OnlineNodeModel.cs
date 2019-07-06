using CanardApp.DataTypes.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardApp.Presentation
{
    class OnlineNodeModel
    {
        public int NodeId { get; set; }
        public string Name { get; set; }
        public NodeStatus.ModeKind Mode { get; set; }
        public NodeStatus.HealthKind Health { get; set; }
        public TimeSpan Uptime { get; set; }
        public ushort VSSC { get; set; }
    }
}
