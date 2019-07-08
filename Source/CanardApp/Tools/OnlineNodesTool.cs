using CanardApp.DataTypes.Protocol;
using CanardApp.Presentation;
using CanardSharp;
using CanardSharp.Dsdl;
using CanardSharp.Dsdl.DataTypes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace CanardApp.Tools
{
    sealed class OnlineNodesTool : IDisposable
    {
        CanardInstance _canard;
        MessageType _nodeStatusMessage;
        ServiceType _getNodeInfoService;

        public OnlineNodesTool(CanardInstance canard)
        {
            _canard = canard;

            canard.MessageReceived += Canard_MessageReceived;
            canard.NodeIDChanged += Canard_NodeIDChanged;

            var typeResolver = canard.TypeResolver;
            ResolveTypes(typeResolver);
        }

        CancellationTokenSource _cts = new CancellationTokenSource();
        LinkedList<Task> _tasks = new LinkedList<Task>();
        object _syncRoot = new object();

        void Canard_NodeIDChanged(object sender, EventArgs e)
        {
            _canard.NodeIDChanged -= Canard_NodeIDChanged;

            foreach (var i in OnlineNodes)
            {
                lock (_syncRoot)
                    _tasks.AddLast(UpdateNodeInfo(i.NodeId));
            }
        }

        async Task UpdateNodeInfo(int nodeId)
        {
            var request = new GetNodeInfo_Request();
            var response = await _canard.SendServiceRequest(nodeId, request, _getNodeInfoService, ct: _cts.Token).ConfigureAwait(false);
            var data = _canard.Serializer.Deserialize<GetNodeInfo_Response>(response.ContentBytes, 0, response.ContentBytes.Length);
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
            if (_canard != null)
            {
                _canard.MessageReceived -= Canard_MessageReceived;
                _canard.NodeIDChanged -= Canard_NodeIDChanged;
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

        void Canard_MessageReceived(object sender, TransferReceivedArgs e)
        {
            if (e.Type == _nodeStatusMessage)
            {
                var data = _canard.Serializer.Deserialize<NodeStatus>(e.ContentBytes, 0, e.ContentBytes.Length);

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

                            if (_canard.NodeID != 0)
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
