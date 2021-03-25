using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Uavcan.NET.Dsdl;
using Uavcan.NET.Dsdl.DataTypes;
using Uavcan.NET.Studio.DataTypes.Protocol;

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
                List<(WeakReference Context, Action<object> Callback)> waitingList;
                lock (_syncRoot)
                {
                    if (_waitingList.TryGetValue(handle, out waitingList))
                        _waitingList.Remove(handle);
                }

                if (waitingList is not null)
                {
                    foreach (var (context, callback) in waitingList)
                    {
                        if (context.IsAlive)
                        {
                            callback(context.Target);
                        }
                    }
                }

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

        ConcurrentDictionary<NodeHandle, INodeDescriptor> _registry = new();

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

        Dictionary<NodeHandle, List<(WeakReference Context, Action<object> Callback)>> _waitingList = new();

        public void WaitForNode(NodeHandle handle, object context, Action<object> callback)
        {
            if (callback is null)
                throw new ArgumentNullException(nameof(callback));

            bool executeCallback = false;
            lock (_syncRoot)
            {
                if (TryGetRegisteredNodeDescriptor(handle, out _))
                {
                    executeCallback = true;
                }
                else
                {
                    CleanupWaitingList();

                    if (!_waitingList.TryGetValue(handle, out var bag))
                    {
                        bag = new();
                        _waitingList.Add(handle, bag);
                    }

                    bag.Add((new WeakReference(context), callback));
                }
            }

            if (executeCallback)
            {
                callback(context);
            }
        }

        private void CleanupWaitingList()
        {
            List<NodeHandle> keysToRemove = null;

            foreach (var kv in _waitingList)
            {
                List<int> valuesToRemove = null;

                var bag = kv.Value;
                for (int i = 0; i < bag.Count; i++)
                {
                    if (!bag[i].Context.IsAlive)
                    {
                        (valuesToRemove ??= new()).Add(i);
                    }
                }

                if (valuesToRemove is not null)
                {
                    for (int i = valuesToRemove.Count - 1; i >= 0; i--)
                    {
                        bag.RemoveAt(i);
                    }
                }

                if (bag.Count == 0)
                {
                    (keysToRemove ??= new List<NodeHandle>()).Add(kv.Key);
                }
            }

            if (keysToRemove is not null)
            {
                foreach (var key in keysToRemove)
                {
                    _waitingList.Remove(key);
                }
            }
        }
    }
}
