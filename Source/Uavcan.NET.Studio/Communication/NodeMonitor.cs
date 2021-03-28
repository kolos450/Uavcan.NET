using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Uavcan.NET.Dsdl;
using Uavcan.NET.Dsdl.DataTypes;
using Uavcan.NET.Studio.DataTypes.Protocol;
using Uavcan.NET.Studio.Framework;

namespace Uavcan.NET.Studio.Communication
{
    sealed class NodeMonitor : INodeMonitor, IDisposable
    {
        private readonly UavcanInstance _uavcan;

        private MessageType _nodeStatusMessage;
        private ServiceType _getNodeInfoService;

        private object _syncRoot = new object();

        public NodeMonitor(UavcanInstance uavcan)
        {
            _uavcan = uavcan ?? throw new ArgumentNullException(nameof(uavcan));

            uavcan.MessageReceived += Uavcan_MessageReceived;
            //uavcan.NodeIDChanged += Uavcan_NodeIDChanged;

            ResolveTypes(uavcan.TypeResolver);

            _nodeWaitingList = new(_registry.ContainsKey);
        }

        void ResolveTypes(IUavcanTypeResolver typeResolver)
        {
            _nodeStatusMessage = (MessageType)typeResolver.ResolveType("uavcan.protocol", "NodeStatus");
            _getNodeInfoService = (ServiceType)typeResolver.ResolveType("uavcan.protocol", "GetNodeInfo");
        }

        private void Uavcan_MessageReceived(object sender, TransferReceivedArgs e)
        {
            if (e.Type == _nodeStatusMessage)
            {
                var nodeStatus = _uavcan.Serializer.Deserialize<NodeStatus>(e.ContentBytes);
                ProcessNodeStatus(e.SourceNodeId, e.ReceivedTime, nodeStatus);
            }
        }

        private void ProcessNodeStatus(int sourceNodeId, DateTime receivedTime, NodeStatus status)
        {
            var handle = new NodeHandle(sourceNodeId);

            bool descriptorCreated = false;
            if (!_registry.TryGetValue(handle, out var descriptor))
            {
                lock (_syncRoot)
                {
                    if (!_registry.TryGetValue(handle, out descriptor))
                    {
                        descriptor = CreateNodeDescriptor(handle, receivedTime);
                        _registry.AddOrUpdate(handle, descriptor, (k, v) => v);
                        descriptorCreated = true;
                    }
                }
            }

            UpdateDescriptor((NodeDescriptor)descriptor, receivedTime, status);

            if (descriptorCreated)
            {
                _nodeWaitingList.AddKey(handle);
                AddActiveNode(handle);
            }
        }

        private void AddActiveNode(NodeHandle handle)
        {
            throw new NotImplementedException();
        }

        private void UpdateDescriptor(NodeDescriptor descriptor, DateTime receivedTime, NodeStatus status)
        {
            descriptor.Updated = receivedTime;
            ((NodeData)descriptor.Status).Update(status);
        }

        private INodeDescriptor CreateNodeDescriptor(NodeHandle handle, DateTime receivedTime)
        {
            return new NodeDescriptor(this, handle)
            {
                Registered = receivedTime,
                Updated = receivedTime,
                Status = new NodeData(),
                Info = new NodeInfo()
            };
        }

        public ReadOnlyObservableCollection<NodeHandle> GetActiveNodes(TimeSpan activeNodeTimeout)
        {
            throw new NotImplementedException();
        }

        readonly ConcurrentDictionary<NodeHandle, INodeDescriptor> _registry = new();

        public bool TryGetRegisteredNodeDescriptor(NodeHandle handle, out INodeDescriptor descriptor)
        {
            return _registry.TryGetValue(handle, out descriptor);
        }

        public INodeDescriptor GetNodeDescriptor(NodeHandle handle)
        {
            return new NodeDescriptorProxy(this, handle);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        readonly WeakWaitingList<NodeHandle> _nodeWaitingList;

        public void WaitForNode(NodeHandle handle, object context, Action<object> callback) =>
            _nodeWaitingList.WaitForKey(handle, context, callback);
    }
}
