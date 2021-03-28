using System;
using System.Collections.Generic;
using System.Text;

namespace Uavcan.NET.Studio.Communication
{
    public readonly struct NodeHandle
    {
        public NodeHandle(int nodeId)
        {
            NodeId = nodeId;
        }

        public readonly int NodeId;

        public override string ToString() =>
            NodeId.ToString();
    }
}
