﻿using Uavcan.NET.Dsdl;
using Uavcan.NET.Dsdl.DataTypes;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uavcan.NET.IO.Can.Drivers;
using Uavcan.NET.IO.Can;

namespace Uavcan.NET
{
    public class UavcanInstance : IDisposable
    {
        readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        readonly DateTime _stopwatchOffset = DateTime.Now;

        readonly CanFramesProcessor _framesProcessor;

        ConcurrentDictionary<long, TaskCompletionSource<TransferReceivedArgs>> _responseTickets =
            new ConcurrentDictionary<long, TaskCompletionSource<TransferReceivedArgs>>();

        public IUavcanTypeResolver TypeResolver { get; }
        public DsdlSerializer Serializer { get; }

        List<ICanInterface> _drivers = new List<ICanInterface>();
        public IEnumerable<ICanInterface> Drivers => _drivers;

        public UavcanInstance(IUavcanTypeResolver typeResolver)
        {
            TypeResolver = typeResolver;
            Serializer = new DsdlSerializer(typeResolver);
            _framesProcessor = new CanFramesProcessor(ShouldAcceptTransfer);
        }

        public void AddDriver(ICanInterface driver)
        {
            driver.MessageReceived += CanDriver_MessageReceived;
            _drivers.Add(driver);
        }

        public void RemoveDriver(ICanInterface driver)
        {
            driver.MessageReceived -= CanDriver_MessageReceived;
            _drivers.Remove(driver);
        }

        public event UnhandledExceptionEventHandler ErrorOccurred;

        void CanDriver_MessageReceived(object sender, CanMessageEventArgs e)
        {
            var nowUs = (ulong)_stopwatch.ElapsedMilliseconds * 1000;
            UavcanRxTransfer transfer;
            try
            {
                transfer = _framesProcessor.HandleRxFrame(e.Message, nowUs).Transfer;
            }
            catch (CanFramesProcessingException ex)
            {
                ErrorOccurred?.Invoke(this, new UnhandledExceptionEventArgs(ex, false));
                return;
            }

            if (transfer == null)
                return;

            switch (transfer.TransferType)
            {
                case UavcanTransferType.Broadcast:
                    ProcessReceivedMessage(transfer);
                    break;
                case UavcanTransferType.Request:
                    ProcessReceivedRequest(transfer);
                    break;
                case UavcanTransferType.Response:
                    ProcessReceivedResponse(transfer);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        byte _nodeId = UavcanConstants.BroadcastNodeId;

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
                if ((value < UavcanConstants.MinNodeId) || (value > UavcanConstants.MaxNodeId))
                    throw new ArgumentOutOfRangeException(nameof(value));

                bool changed = _nodeId != value;
                _nodeId = value;
                if (changed)
                    NodeIDChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        void ProcessReceivedResponse(UavcanRxTransfer transfer)
        {
            var transferDescriptor = GetTransferDescriptor(
                    UavcanTransferType.Request,
                    (int)transfer.DataTypeId,
                    NodeID,
                    transfer.SourceNodeId);
            var responseTickedId = transferDescriptor | (transfer.TransferId << 32);
            if (_responseTickets.TryRemove(responseTickedId, out var ticket))
            {
                var args = CreateTransferReceivedArgs(transfer);
                ticket.TrySetResult(args);
            }
        }

        void ProcessReceivedRequest(UavcanRxTransfer transfer)
        {
            var args = CreateTransferReceivedArgs(transfer);
            RequestReceived?.Invoke(this, args);
        }

        void ProcessReceivedMessage(UavcanRxTransfer transfer)
        {
            var args = CreateTransferReceivedArgs(transfer);
            MessageReceived?.Invoke(this, args);
        }

        TransferReceivedArgs CreateTransferReceivedArgs(UavcanRxTransfer transfer)
        {
            var typeKind = transfer.TransferType == UavcanTransferType.Broadcast ?
                UavcanTypeKind.Message :
                UavcanTypeKind.Service;
            var uavcanType = TypeResolver.TryResolveType((int)transfer.DataTypeId, typeKind);
            if (uavcanType == null)
                throw new InvalidOperationException($"Cannot resolve uavcan type with id = {transfer.DataTypeId}.");

            return new TransferReceivedArgs
            {
                Type = uavcanType,
                ContentBytes = transfer.Payload,
                Priority = transfer.Priority,
                ReceivedTime = _stopwatchOffset + TimeSpan.FromMilliseconds(transfer.TimestampUsec / 1000),
                SourceNodeId = transfer.SourceNodeId,
                TransferId = transfer.TransferId,
                TransferType = transfer.TransferType,
            };
        }

        bool ShouldAcceptTransfer(out ulong dataTypeSignature, uint dataTypeId, UavcanTransferType transferType, byte sourceNodeId, byte destinationNodeId)
        {
            dataTypeSignature = 0;

            if (transferType != UavcanTransferType.Broadcast && destinationNodeId != NodeID)
                return false;

            var typeKind = transferType == UavcanTransferType.Broadcast ?
                UavcanTypeKind.Message :
                UavcanTypeKind.Service;
            var type = TypeResolver.TryResolveType((int)dataTypeId, typeKind);
            if (type == null)
                return false;

            var signature = type.GetDataTypeSignature()
                ?? throw new InvalidOperationException($"Cannot get data type signature for '{type}'.");

            dataTypeSignature = signature;

            return true;
        }

        bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_drivers != null)
                    {
                        foreach (var driver in _drivers)
                            driver.Dispose();
                        _drivers = null;
                    }

                    if (_responseTickets != null)
                    {
                        var values = _responseTickets.Values.ToList();
                        foreach (var i in values)
                        {
                            i.TrySetCanceled();
                        }
                        _responseTickets = null;
                    }
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        static int GetTransferDescriptor(UavcanTransferType transferType, int dataTypeId, int sourceNodeId, int destinationNodeId)
        {
            return (((int)transferType) << 30) |
                ((sourceNodeId & 0x7F) << 23) |
                ((destinationNodeId & 0x7F) << 16) |
                (dataTypeId & 0xFFFF);
        }

        readonly ConcurrentDictionary<int, byte> _transferIdRegistry = new();

        public event EventHandler<TransferReceivedArgs> MessageReceived;
        public event EventHandler<TransferReceivedArgs> RequestReceived;

        public TxToken SendBroadcastMessage(
            object value,
            MessageType valueType = null,
            UavcanPriority priority = UavcanPriority.Medium)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (valueType == null)
                valueType = GetUavcanType<MessageType>(value);

            DsdlType dsdlType = valueType;

            var buffer = _arrayPool.Rent(dsdlType.MaxBitlen);
            try
            {
                int payloadLen = Serializer.Serialize(value, dsdlType, buffer);

                var transferDescriptor = GetTransferDescriptor(
                    UavcanTransferType.Request,
                    valueType.Meta.DefaultDTID.Value,
                    NodeID,
                    0);

                byte transferId = GetNextTransferId(transferDescriptor);

                var frames = CanFramesGenerator.Broadcast(valueType.GetDataTypeSignature().Value,
                    valueType.Meta.DefaultDTID.Value,
                    transferId,
                    NodeID,
                    priority,
                    buffer,
                    0,
                    payloadLen);

                var ticket = CreateTicket();

                SendFrames(frames, ticket);

                return ticket;
            }
            finally
            {
                _arrayPool.Return(buffer);
            }
        }

        private TxToken CreateTicket()
        {
            return new TxToken(_drivers);
        }

        readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;

        public async Task<TransferReceivedArgs> SendServiceRequest(
            int destinationNodeId,
            object value,
            ServiceType valueType = null,
            UavcanPriority priority = UavcanPriority.Medium,
            CancellationToken ct = default)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (valueType == null)
                valueType = GetUavcanType<ServiceType>(value);

            var dsdlType = valueType.Request;

            TaskCompletionSource<TransferReceivedArgs> ticket;
            var buffer = _arrayPool.Rent((dsdlType.MaxBitlen + 7) / 8);
            try
            {
                int payloadLen = Serializer.Serialize(value, dsdlType, buffer);

                var transferDescriptor = GetTransferDescriptor(
                    UavcanTransferType.Request,
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
                        UavcanRequestResponse.Request,
                        buffer,
                        0,
                        payloadLen);

                SendFrames(frames);

                var responseTickedId = transferDescriptor | (transferId << 32);
                ticket = _responseTickets.GetOrAdd(responseTickedId, _ => new TaskCompletionSource<TransferReceivedArgs>());
            }
            finally
            {
                _arrayPool.Return(buffer);
            }

            using (ct.Register(() => ticket.TrySetCanceled()))
            {
                return await ticket.Task.ConfigureAwait(false);
            }
        }

        private byte GetNextTransferId(int transferDescriptor)
        {
            return (byte)((_transferIdRegistry.AddOrUpdate(transferDescriptor, 1, (_, id) => (byte)((id + 1) & 0x1F)) - 1) & 0x1F);
        }

        void SendFrames(IEnumerable<CanFrame> frames, TxToken ticket = null)
        {
            var drivers = _drivers;
            if (drivers is not null)
            {
                foreach (var frame in frames)
                {
                    foreach (var driver in _drivers)
                    {
                        ticket?.Add(frame, driver);
                        driver.Send(frame);
                    }
                }
            }
        }

        T GetUavcanType<T>(object value) where T : class, IUavcanType
        {
            var contract = Serializer.ContractResolver.ResolveContract(value.GetType());
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
            UavcanPriority priority = UavcanPriority.Medium)
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
                int payloadLen = Serializer.Serialize(value, dsdlType, buffer);
                var transferId = request.TransferId;

                var frames = CanFramesGenerator.RequestOrRespond(destinationNodeId,
                    uavcanType.GetDataTypeSignature().Value,
                    uavcanType.Meta.DefaultDTID.Value,
                    transferId,
                    NodeID,
                    priority,
                    UavcanRequestResponse.Response,
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