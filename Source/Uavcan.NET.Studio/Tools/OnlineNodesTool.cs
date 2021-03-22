using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Uavcan.NET.Dsdl;
using Uavcan.NET.Dsdl.DataTypes;
using Uavcan.NET.Studio.DataTypes.Protocol;
using Uavcan.NET.Studio.Presentation;

namespace Uavcan.NET.Studio.Tools
{
    sealed class OnlineNodesTool : IDisposable
    {
        UavcanInstance _uavcan;
        MessageType _nodeStatusMessage;
        ServiceType _getNodeInfoService;

        public OnlineNodesTool(UavcanInstance uavcan)
        {
            _uavcan = uavcan;

            uavcan.MessageReceived += Uavcan_MessageReceived;
            uavcan.NodeIDChanged += Uavcan_NodeIDChanged;

            var typeResolver = uavcan.TypeResolver;
            ResolveTypes(typeResolver);
        }

        CancellationTokenSource _cts = new CancellationTokenSource();
        LinkedList<Task> _tasks = new LinkedList<Task>();
        object _syncRoot = new object();

        void Uavcan_NodeIDChanged(object sender, EventArgs e)
        {
            _uavcan.NodeIDChanged -= Uavcan_NodeIDChanged;

            foreach (var i in OnlineNodes)
            {
                lock (_syncRoot)
                    _tasks.AddLast(UpdateNodeInfo(i.NodeId));
            }
        }

        async Task UpdateNodeInfo(int nodeId)
        {
            var request = new GetNodeInfo_Request();
            var response = await _uavcan.SendServiceRequest(nodeId, request, _getNodeInfoService, ct: _cts.Token).ConfigureAwait(false);
            var data = _uavcan.Serializer.Deserialize<GetNodeInfo_Response>(response.ContentBytes);
            Application.Current?.Dispatcher.Invoke(() =>
            {
                if (_nodesLookup.TryGetValue(response.SourceNodeId, out var model))
                    UpdateNodeStatus(response.ReceivedTime, data, model);
            });
        }

        void ResolveTypes(IUavcanTypeResolver typeResolver)
        {
            _nodeStatusMessage = (MessageType)typeResolver.ResolveType("uavcan.protocol", "NodeStatus");
            _getNodeInfoService = (ServiceType)typeResolver.ResolveType("uavcan.protocol", "GetNodeInfo");
        }

        public void Dispose()
        {
            if (_uavcan != null)
            {
                _uavcan.MessageReceived -= Uavcan_MessageReceived;
                _uavcan.NodeIDChanged -= Uavcan_NodeIDChanged;
            }

            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }

            if (_tasks != null)
            {
                Task.WhenAll(_tasks.ToArray()).GetAwaiter().GetResult();
                _tasks = null;
            }
        }

        public ObservableCollection<OnlineNodeModel> OnlineNodes { get; } = new ObservableCollection<OnlineNodeModel>();
        Dictionary<int, OnlineNodeModel> _nodesLookup = new Dictionary<int, OnlineNodeModel>();

        void Uavcan_MessageReceived(object sender, TransferReceivedArgs e)
        {
            if (e.Type == _nodeStatusMessage)
            {
                var data = _uavcan.Serializer.Deserialize<NodeStatus>(e.ContentBytes);

                Application.Current?.Dispatcher.BeginInvoke((Action)(() =>
                {
                    OnlineNodeModel model;

                    lock (_syncRoot)
                    {
                        if (!_nodesLookup.TryGetValue(e.SourceNodeId, out model))
                        {
                            model = new OnlineNodeModel
                            {
                                NodeId = e.SourceNodeId
                            };
                            _nodesLookup[e.SourceNodeId] = model;
                            OnlineNodes.Add(model);

                            if (_uavcan.NodeID != 0)
                            {
                                CleanupTasks();
                                _tasks.AddLast(UpdateNodeInfo(e.SourceNodeId));
                            }
                        }
                    }

                    UpdateNodeStatus(e.ReceivedTime, data, model);

                }));
            }
        }

        void CleanupTasks()
        {
            if (_tasks.Count == 0)
                return;

            var node = _tasks.First;
            while (node != null)
            {
                var next = node.Next;
                if (node.Value.IsCompleted)
                {
                    node.Value.GetAwaiter().GetResult();
                    _tasks.Remove(node);
                }
                node = next;
            }
        }

        static void UpdateNodeStatus(DateTime receivedTime, NodeStatus data, OnlineNodeModel model)
        {
            model.Updated = receivedTime;
            model.Health = data.Health;
            model.Mode = data.Mode;
            model.Uptime = TimeSpan.FromSeconds(data.UptimeSec);
            model.VSSC = data.VendorSpecificStatusCode;
        }

        static void UpdateNodeStatus(DateTime receivedTime, GetNodeInfo_Response data, OnlineNodeModel model)
        {
            model.Updated = receivedTime;
            model.Name = data.Name;
        }
    }
}
