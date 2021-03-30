using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

        private TaskBag _requestBag = new();
        private readonly object _syncRoot = new();

        private readonly ConcurrentDictionary<NodeHandle, INodeDescriptor> _registry = new();
        private readonly WeakWaitingList<NodeHandle> _nodeWaitingList;

        public NodeMonitor(UavcanInstance uavcan)
        {
            _uavcan = uavcan ?? throw new ArgumentNullException(nameof(uavcan));

            uavcan.MessageReceived += Uavcan_MessageReceived;

            if (uavcan.NodeID == 0)
                uavcan.NodeIDChanged += Uavcan_NodeIDChanged;

            ResolveTypes(uavcan.TypeResolver);

            _nodeWaitingList = new(_registry.ContainsKey);
        }

        private void ResolveTypes(IUavcanTypeResolver typeResolver)
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

            _requestBag.FinalizeCompletedTasks();
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
                        descriptor = new NodeDescriptor(this, handle, receivedTime);
                        _registry.AddOrUpdate(handle, descriptor, (k, v) => v);
                        descriptorCreated = true;

                        if (_uavcan.NodeID != 0)
                        {
                            _requestBag.Add(UpdateNodeInfo(handle));
                        }
                    }
                }
            }

            UpdateDescriptor((NodeDescriptor)descriptor, receivedTime, status);

            if (descriptorCreated)
            {
                _nodeWaitingList.AddKey(handle);
                AddActiveNode(handle);
            }
            else
            {
                PulseActiveNode(handle);
            }
        }

        private void UpdateDescriptor(NodeDescriptor descriptor, DateTime receivedTime, NodeStatus status)
        {
            lock (descriptor)
            {
                descriptor.Updated = receivedTime;
                ((NodeData)descriptor.Status).Update(status);
            }
        }

        public bool TryGetRegisteredNodeDescriptor(NodeHandle handle, out INodeDescriptor descriptor) =>
            _registry.TryGetValue(handle, out descriptor);

        public INodeDescriptor GetNodeDescriptor(NodeHandle handle) =>
            new NodeDescriptorProxy(this, handle);

        public void Dispose()
        {
            if (_uavcan != null)
            {
                _uavcan.MessageReceived -= Uavcan_MessageReceived;
                _uavcan.NodeIDChanged -= Uavcan_NodeIDChanged;
            }

            if (_requestBag is not null)
            {
                _requestBag.Dispose();
                _requestBag = null;
            }

            if (_activeNodesTimer is not null)
            {
                _activeNodesTimer.Dispose();
                _activeNodesTimer = null;
            }
        }

        public void WaitForNode(NodeHandle handle, object context, Action<object> callback) =>
            _nodeWaitingList.WaitForKey(handle, context, callback);

        private void Uavcan_NodeIDChanged(object sender, EventArgs e)
        {
            _uavcan.NodeIDChanged -= Uavcan_NodeIDChanged;

            _requestBag.Add(Task.Factory.StartNew(() =>
            {
                foreach (var handle in _registry.Keys)
                {
                    _requestBag.Add(UpdateNodeInfo(handle));
                }
            }));
        }

        private async Task UpdateNodeInfo(NodeHandle handle)
        {
            try
            {
                var request = new GetNodeInfo_Request();
                var response = await _uavcan.SendServiceRequest(
                    handle.NodeId,
                    request,
                    _getNodeInfoService,
                    ct: _requestBag.CancellationToken)
                    .ConfigureAwait(false);
                var data = _uavcan.Serializer.Deserialize<GetNodeInfo_Response>(response.ContentBytes);

                if (_registry.TryGetValue(handle, out var descriptor))
                {
                    var nodeDescriptor = (NodeDescriptor)descriptor;
                    lock (descriptor)
                    {
                        nodeDescriptor.Updated = response.ReceivedTime;
                        ((NodeData)nodeDescriptor.Status).Update(data.Status);
                        ((NodeInfo)nodeDescriptor.Info).Update(data);
                    }
                }
            }
            catch (TaskCanceledException)
            { }
        }

        readonly ConcurrentDictionary<NodeHandle, bool> _inactiveNodes = new();
        readonly ObservableCollection<NodeHandle> _activeNodes = new();
        ReadOnlyObservableCollection<NodeHandle> _activeNodesRO;

        public ReadOnlyObservableCollection<NodeHandle> GetActiveNodes() =>
            _activeNodesRO ??= new(_activeNodes);

        private void PulseActiveNode(NodeHandle handle)
        {
            if (_inactiveNodes.TryGetValue(handle, out var removed) && removed)
            {
                lock (_syncRoot)
                {
                    _inactiveNodes[handle] = false;
                    bool added = false;
                    foreach (var collection in ActiveNodesCollections)
                    {
                        if (collection.Add(handle))
                            added = true;
                    }

                    if (added)
                    {
                        ResetActiveNodesTimer();
                    }
                }
            }
        }

        private void AddActiveNode(NodeHandle handle)
        {
            lock (_syncRoot)
            {
                _activeNodes.Add(handle);

                CleanupActiveNodesCollections();
                foreach (var collection in ActiveNodesCollections)
                {
                    collection.Add(handle);
                }

                if (_activeNodesTimer?.Enabled == false) // Check if any observable collection was acquired.
                {
                    ResetActiveNodesTimer();
                }
            }
        }

        private TimeSpan _minActiveNodesTimeout;
        private System.Timers.Timer _activeNodesTimer;
        private readonly Dictionary<TimeSpan, WeakReference<ActiveNodesCollection>> _activeNodesCollections = new();

        private void ResetActiveNodesTimer() =>
            ResetActiveNodesTimer(DateTimeOffset.Now - _minActiveNodesTimeout);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsActiveNode(INodeDescriptor descriptor, DateTimeOffset threshold) =>
            descriptor.Updated > threshold;

        private void ResetActiveNodesTimer(DateTimeOffset expirationTime)
        {
            _activeNodesTimer.Stop();

            var expiresNext = _registry
                .OrderByDescending(kv => kv.Value.Updated)
                .LastOrDefault(kv => IsActiveNode(kv.Value, expirationTime))
                .Value?.Updated;

            if (expiresNext is not null)
            {
                var interval = expiresNext.Value - expirationTime;
                var intervalMs = interval.TotalMilliseconds;
                if (intervalMs < int.MaxValue)
                {
                    _activeNodesTimer.Interval = interval.TotalMilliseconds;
                    _activeNodesTimer.Start();
                }
            }
        }

        public ReadOnlyObservableCollection<NodeHandle> GetActiveNodes(TimeSpan timeout)
        {
            timeout = SnapTimeout(timeout);

            lock (_syncRoot)
            {
                if (_activeNodesTimer is null)
                {
                    _activeNodesTimer = new() { AutoReset = false };
                    _activeNodesTimer.Elapsed += (o, e) =>
                    {
                        lock (_syncRoot)
                        {
                            var now = DateTimeOffset.Now;

                            foreach (var collection in ActiveNodesCollections)
                            {
                                collection.Filter(now);
                            }

                            ResetActiveNodesTimer(now - _minActiveNodesTimeout);
                        }
                    };
                }

                CleanupActiveNodesCollections();

                if (!_activeNodesCollections.TryGetValue(timeout, out var wref) ||
                    !wref.TryGetTarget(out var instance))
                {
                    instance = new ActiveNodesCollection(this, _activeNodes, timeout);
                    _activeNodesCollections[timeout] = new WeakReference<ActiveNodesCollection>(instance);

                    var minTimeout = TimeSpan.MaxValue;
                    foreach (var collection in ActiveNodesCollections)
                    {
                        if (collection.Timeout < minTimeout)
                            minTimeout = collection.Timeout;
                    }
                    _minActiveNodesTimeout = minTimeout;

                    ResetActiveNodesTimer();
                }

                return new ReadOnlyObservableCollection<NodeHandle>(instance);
            }
        }

        private IEnumerable<ActiveNodesCollection> ActiveNodesCollections
        {
            get
            {
                foreach (var kv in _activeNodesCollections)
                {
                    if (kv.Value.TryGetTarget(out var collection))
                        yield return collection;
                }
            }
        }

        private static TimeSpan SnapTimeout(TimeSpan timeout)
        {
            var ticks = timeout.Ticks;
            var step = TimeSpan.TicksPerMillisecond * 100;
            if (ticks < step)
                ticks = step;
            else
                ticks = (ticks / step) * step;
            return TimeSpan.FromTicks(ticks);
        }

        private void CleanupActiveNodesCollections()
        {
            List<TimeSpan> toRemove = null;
            foreach (var kv in _activeNodesCollections)
            {
                if (!kv.Value.TryGetTarget(out _))
                    (toRemove ??= new()).Add(kv.Key);
            }

            if (toRemove is not null)
            {
                foreach (var k in toRemove)
                    _activeNodesCollections.Remove(k);
            }
        }

        private sealed class ActiveNodesCollection : ObservableCollection<NodeHandle>
        {
            readonly NodeMonitor _monitor;
            readonly HashSet<NodeHandle> _nodesSet = new();

            public ActiveNodesCollection(
                NodeMonitor monitor,
                IEnumerable<NodeHandle> nodes,
                TimeSpan timeout)
            {
                _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));

                Timeout = timeout;

                var expiresAt = DateTimeOffset.Now - timeout;
                foreach (var node in nodes)
                {
                    if (IsActiveNode(monitor._registry[node], expiresAt))
                        Add(node);
                }
            }

            public new bool Add(NodeHandle handle)
            {
                if (_nodesSet.Add(handle))
                {
                    base.Add(handle);
                    return true;
                }

                return false;
            }

            public TimeSpan Timeout { get; }

            public void Filter(DateTimeOffset now)
            {
                var threshold = now - Timeout;

                List<int> toRemove = null;
                for (int i = 0; i < Count; i++)
                {
                    var handle = this[i];
                    if (_monitor.TryGetRegisteredNodeDescriptor(handle, out var descriptor) &&
                        !IsActiveNode(descriptor, threshold))
                    {
                        (toRemove ??= new List<int>()).Add(i);
                        _monitor._inactiveNodes[handle] = true;
                        _nodesSet.Remove(handle);
                    }
                }

                if (toRemove is not null)
                {
                    for (int i = toRemove.Count - 1; i >= 0; i--)
                    {
                        RemoveAt(toRemove[i]);
                    }
                }
            }
        }
    }
}
