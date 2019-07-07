﻿using CanardSharp;
using CanardSharp.Drivers;
using CanardSharp.Dsdl;
using CanardSharp.Dsdl.DataTypes;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CanardSharp
{
    public class CanardInstance : IDisposable
    {
        readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        readonly DateTime _stopwatchOffset = DateTime.Now;

        ICanDriver _canDriver;
        IUavcanTypeResolver _typeResolver;
        DsdlSerializer _serializer;
        CanFramesProcessor _framesProcessor;

        ConcurrentDictionary<long, ResponseTicket> _responseTickets = new ConcurrentDictionary<long, ResponseTicket>();

        public IUavcanTypeResolver TypeResolver => _typeResolver;
        public DsdlSerializer Serializer => _serializer;

        public CanardInstance(ICanDriver canDriver, IUavcanTypeResolver typeResolver)
        {
            _typeResolver = typeResolver;
            _serializer = new DsdlSerializer(typeResolver);
            _framesProcessor = new CanFramesProcessor(CanardShouldAcceptTransfer);

            _canDriver = canDriver;

            _canDriver.MessageReceived += CanDriver_MessageReceived;
        }

        void CanDriver_MessageReceived(object sender, CanMessageEventArgs e)
        {
            var nowUs = (ulong)_stopwatch.ElapsedMilliseconds * 1000;
            var transfer = _framesProcessor.HandleRxFrame(e.Message, nowUs).Transfer;
            if (transfer == null)
                return;

            switch (transfer.TransferType)
            {
                case CanardTransferType.CanardTransferTypeBroadcast:
                    ProcessReceivedMessage(transfer);
                    break;
                case CanardTransferType.CanardTransferTypeRequest:
                    ProcessReceivedRequest(transfer);
                    break;
                case CanardTransferType.CanardTransferTypeResponse:
                    ProcessReceivedResponse(transfer);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        byte _nodeId = CanardConstants.BroadcastNodeId;

        public event EventHandler NodeIDChanged;

        /// <summary>
        /// Node ID of the local node.
        /// </summary>
        /// <remarks>
        /// Getter returns zero (broadcast) if the node ID is not set, i.e. if the local node is anonymous.
        /// Node ID can be assigned only once.
        /// </remarks>
        public byte NodeID
        {
            get => _nodeId;

            set
            {
                if ((value < CanardConstants.MinNodeId) || (value > CanardConstants.MaxNodeId))
                    throw new ArgumentOutOfRangeException(nameof(value));

                bool changed = _nodeId != value;
                _nodeId = value;
                if (changed)
                    NodeIDChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        void ProcessReceivedResponse(CanardRxTransfer transfer)
        {
            var transferDescriptor = GetTransferDescriptor(
                    CanardTransferType.CanardTransferTypeRequest,
                    (int)transfer.DataTypeId,
                    NodeID,
                    transfer.SourceNodeId);
            var responseTickedId = transferDescriptor | (transfer.TransferId << 32);
            if (_responseTickets.TryRemove(responseTickedId, out var ticket))
            {
                var args = CreateTransferReceivedArgs(transfer);
                ticket.SetResponse(args);
                ticket.Dispose();
            }
        }

        void ProcessReceivedRequest(CanardRxTransfer transfer)
        {
            var args = CreateTransferReceivedArgs(transfer);
            RequestReceived?.Invoke(this, args);
        }

        void ProcessReceivedMessage(CanardRxTransfer transfer)
        {
            var args = CreateTransferReceivedArgs(transfer);
            MessageReceived?.Invoke(this, args);
        }

        TransferReceivedArgs CreateTransferReceivedArgs(CanardRxTransfer transfer)
        {
            var uavcanType = _typeResolver.TryResolveType((int)transfer.DataTypeId);
            if (uavcanType == null)
                throw new InvalidOperationException($"Cannot resolve uavcan type with id = {transfer.DataTypeId}.");

            var scheme = GetScheme(uavcanType, transfer.TransferType);

            return new TransferReceivedArgs
            {
                Type = uavcanType,
                Content = CreateUnknownObjectFactory(transfer.Payload, scheme),
                ContentBytes = transfer.Payload,
                Priority = transfer.Priority,
                ReceivedTime = _stopwatchOffset + TimeSpan.FromMilliseconds(transfer.TimestampUsec / 1000),
                SourceNodeId = transfer.SourceNodeId,
                TransferId = transfer.TransferId
            };
        }

        private Lazy<object> CreateUnknownObjectFactory(byte[] payload, DsdlType scheme)
        {
            return new Lazy<object>(() =>
                _serializer.Deserialize(payload, 0, payload.Length, scheme));
        }

        static CompositeDsdlTypeBase GetScheme(IUavcanType uavcanType, CanardTransferType transferType)
        {
            switch (transferType)
            {
                case CanardTransferType.CanardTransferTypeBroadcast:
                    return uavcanType as MessageType;
                case CanardTransferType.CanardTransferTypeRequest:
                    return (uavcanType as ServiceType).Request;
                case CanardTransferType.CanardTransferTypeResponse:
                    return (uavcanType as ServiceType).Response;
                default:
                    throw new ArgumentOutOfRangeException(nameof(transferType));
            }
        }

        bool CanardShouldAcceptTransfer(out ulong dataTypeSignature, uint dataTypeId, CanardTransferType transferType, byte sourceNodeId, byte destinationNodeId)
        {
            dataTypeSignature = 0;

            if (transferType != CanardTransferType.CanardTransferTypeBroadcast && destinationNodeId != NodeID)
                return false;

            var type = _typeResolver.TryResolveType((int)dataTypeId);
            if (type == null)
                return false;

            var signature = type.GetDataTypeSignature()
                ?? throw new InvalidOperationException($"Cannot get data type signature for '{type}'.");

            dataTypeSignature = signature;

            return true;
        }

        public void Dispose()
        {
            if (_canDriver != null)
            {
                _canDriver.Dispose();
                _canDriver = null;
            }
            if (_responseTickets != null)
            {
                var values = _responseTickets.Values.ToList();
                foreach (var i in values)
                {
                    i.Cancel();
                    i.Dispose();
                }
                _responseTickets = null;
            }
        }

        static int GetTransferDescriptor(CanardTransferType transferType, int dataTypeId, int sourceNodeId, int destinationNodeId)
        {
            return (((int)transferType) << 30) |
                ((sourceNodeId & 0x7F) << 23) |
                ((destinationNodeId & 0x7F) << 16) |
                (dataTypeId & 0xFFFF);
        }

        ConcurrentDictionary<int, byte> _transferIdRegistry = new ConcurrentDictionary<int, byte>();

        public event EventHandler<TransferReceivedArgs> MessageReceived;
        public event EventHandler<TransferReceivedArgs> RequestReceived;

        public void SendBroadcastMessage(
            object value,
            MessageType valueType = null,
            CanardPriority priority = CanardPriority.Medium)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (valueType == null)
                valueType = GetUavcanType<MessageType>(value);

            DsdlType dsdlType = valueType;

            var buffer = _arrayPool.Rent(dsdlType.MaxBitlen);
            try
            {
                int payloadLen = _serializer.Serialize(value, dsdlType, buffer);

                var transferDescriptor = GetTransferDescriptor(
                    CanardTransferType.CanardTransferTypeRequest,
                    valueType.Meta.DefaultDTID.Value,
                    NodeID,
                    0);

                byte transferId = GetNextTransferId(transferDescriptor);

                var frames = CanFramesGenerator.Broadcast(valueType.GetDataTypeSignature().Value,
                    valueType.Meta.DefaultDTID.Value,
                    (byte)(transferId - 1),
                    NodeID,
                    priority,
                    buffer,
                    0,
                    payloadLen);

                SendFrames(frames);
            }
            finally
            {
                _arrayPool.Return(buffer);
            }
        }

        readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;

        public Task<TransferReceivedArgs> SendServiceRequest(
            int destinationNodeId,
            object value,
            ServiceType valueType = null,
            CanardPriority priority = CanardPriority.Medium,
            CancellationToken ct = default)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (valueType == null)
                valueType = GetUavcanType<ServiceType>(value);

            var dsdlType = valueType.Request;

            ResponseTicket ticket;
            var buffer = _arrayPool.Rent(dsdlType.MaxBitlen);
            try
            {
                int payloadLen = _serializer.Serialize(value, dsdlType, buffer);

                var transferDescriptor = GetTransferDescriptor(
                    CanardTransferType.CanardTransferTypeRequest,
                    valueType.Meta.DefaultDTID.Value,
                    NodeID,
                    destinationNodeId);

                byte transferId = GetNextTransferId(transferDescriptor);

                var frames = CanFramesGenerator.RequestOrRespond(destinationNodeId,
                        valueType.GetDataTypeSignature().Value,
                        valueType.Meta.DefaultDTID.Value,
                        transferId,
                        NodeID,
                        priority,
                        CanardRequestResponse.CanardRequest,
                        buffer,
                        0,
                        payloadLen);

                SendFrames(frames);

                ticket = new ResponseTicket();
                var responseTickedId = transferDescriptor | (transferId << 32);
                if (!_responseTickets.TryAdd(responseTickedId, ticket))
                {
                    ticket.Dispose();
                    throw new InvalidOperationException($"Transfer descriptor {responseTickedId} is registered already.");
                }
            }
            finally
            {
                _arrayPool.Return(buffer);
            }

            return ticket.WaitForResponse(ct);
        }

        private byte GetNextTransferId(int transferDescriptor)
        {
            return (byte)(_transferIdRegistry.AddOrUpdate(transferDescriptor, 1, (_, id) => (byte)(id + 1)) - 1);
        }

        void SendFrames(IEnumerable<CanFrame> frames)
        {
            foreach (var frame in frames)
            {
                _canDriver.Send(frame);
            }
        }

        T GetUavcanType<T>(object value) where T : class, IUavcanType
        {
            var contract = _serializer.ContractResolver.ResolveContract(value.GetType());
            if (!(contract.UavcanType is T valueType))
                throw new ArgumentException($"Cannot resolve Uavcan type for '{value.GetType().FullName}'.", nameof(value));
            if (valueType.Meta?.DefaultDTID == null)
                throw new ArgumentException(
                    $"Uavcan type '{valueType.Meta?.FullName}' resolved for '{value.GetType().FullName}' has no data type id defined.");
            return valueType;
        }

        public void SendServiceResponse(
            int destinationNodeId,
            object value,
            TransferReceivedArgs request,
            CanardPriority priority = CanardPriority.Medium)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var uavcanType = request.Type;
            var dsdlType = (uavcanType as ServiceType)?.Response;

            var buffer = _arrayPool.Rent(dsdlType.MaxBitlen);
            try
            {
                int payloadLen = _serializer.Serialize(value, dsdlType, buffer);
                var transferId = request.TransferId;

                var frames = CanFramesGenerator.RequestOrRespond(destinationNodeId,
                    uavcanType.GetDataTypeSignature().Value,
                    uavcanType.Meta.DefaultDTID.Value,
                    transferId,
                    NodeID,
                    priority,
                    CanardRequestResponse.CanardResponse,
                    buffer,
                    0,
                    payloadLen);

                SendFrames(frames);
            }
            finally
            {
                _arrayPool.Return(buffer);
            }
        }
    }
}