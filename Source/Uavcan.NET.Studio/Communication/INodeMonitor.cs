using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Uavcan.NET.Studio.Communication
{
    public interface INodeMonitor
    {
        INodeDescriptor GetNodeDescriptor(NodeHandle handle);
        ReadOnlyObservableCollection<NodeHandle> GetActiveNodes();
        ReadOnlyObservableCollection<NodeHandle> GetActiveNodes(TimeSpan activeNodeTimeout);
    }
}
