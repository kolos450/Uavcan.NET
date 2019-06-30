using CanardSharp;
using CanardSharp.Drivers.Slcan;
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

namespace CanardApp.IO
{
    class CanardInstance : CanardInstanceBase, IDisposable
    {
        readonly string _portName;
        readonly int _bitRate;
        readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        readonly DateTime _stopwatchOffset = DateTime.Now;

        UsbTin _usbTin;
        FileSystemUavcanTypeResolver _typeResolver;
        DsdlSerializer _serializer;

        ConcurrentDictionary<long, ResponseTicket> _responseTickets = new ConcurrentDictionary<long, ResponseTicket>();

        public IUavcanTypeResolver TypeResolver => _typeResolver;
        public DsdlSerializer Serializer => _serializer;

        public CanardInstance(string portName, int bitRate)
        {
            _portName = portName;
            _bitRate = bitRate;

            _typeResolver = new FileSystemUavcanTypeResolver(@"C:\Sources\libuavcan\dsdl\kplc");
            _serializer = new DsdlSerializer(_typeResolver);

            _usbTin = new UsbTin();
            _usbTin.Connect(portName);
            _usbTin.OpenCanChannel(bitRate, UsbTinOpenMode.Active);

            Logger.Info($"Connected to {portName}, bit rate = {bitRate}.");

            _usbTin.MessageReceived += _usbTin_MessageReceived;
        }

        const ulong CANARD_RECOMMENDED_STALE_TRANSFER_CLEANUP_INTERVAL_USEC = 1000000U;
        ulong previousCleanupTime = 0;

        void _usbTin_MessageReceived(object sender, CanMessageReceivedEventArgs e)
        {
            var nowUs = (ulong)_stopwatch.ElapsedMilliseconds * 1000;
            var transfer = HandleRxMessage(e.Message, nowUs);
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

        CanardRxTransfer HandleRxMessage(CanMessage msg, ulong nowUs)
        {
            if (nowUs - previousCleanupTime >= CANARD_RECOMMENDED_STALE_TRANSFER_CLEANUP_INTERVAL_USEC)
                CleanupStaleTransfers(nowUs);

            return HandleRxFrame(ConvertMessage(msg), nowUs);
        }

        public override bool CanardShouldAcceptTransfer(out ulong out_data_type_signature, uint data_type_id, CanardTransferType transfer_type, byte source_node_id)
        {
            out_data_type_signature = 0;

            var type = _typeResolver.TryResolveType((int)data_type_id);
            if (type == null)
                return false;

            var signature = type.GetDataTypeSignature()
                ?? throw new InvalidOperationException($"Cannot get data type signature for '{type}'.");

            out_data_type_signature = signature;

            return true;
        }

        public void Dispose()
        {
            if (_usbTin != null)
            {
                _usbTin.Dispose();
                _usbTin = null;
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

        async Task SendFramesAsync()
        {
            var msg = PeekTxQueue();
            while (msg != null)
            {
                var usbTinMsg = ConvertMessage(msg);
                await _usbTin.SendAsync(usbTinMsg).ConfigureAwait(false);

                PopTxQueue();
                msg = PeekTxQueue();
            }
        }

        CanMessage ConvertMessage(CanardCANFrame msg)
        {
            var buffer = new byte[msg.DataLength];
            Buffer.BlockCopy(msg.Data, 0, buffer, 0, buffer.Length);
            return new CanMessage((int)msg.Id.Value, buffer);
        }

        CanardCANFrame ConvertMessage(CanMessage msg)
        {
            return new CanardCANFrame
            {
                Id = new CanId((uint)msg.Id),
                Data = msg.Data,
                DataLength = msg.Data.Length
            };
        }

        static int GetTransferDescriptor(CanardTransferType transferType, int dataTypeId, int sourceNodeId, int destinationNodeId)
        {
            return (((int)transferType) << 30) |
                ((sourceNodeId & 0x7F) << 23) |
                ((destinationNodeId & 0x7F) << 16) |
                (dataTypeId & 0xFFFF);
        }

        Dictionary<int, byte> _transferIdRegistry = new Dictionary<int, byte>();

        public event EventHandler<TransferReceivedArgs> MessageReceived;
        public event EventHandler<TransferReceivedArgs> RequestReceived;

        public Task SendBroadcastMessage(
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

                var transferId = _transferIdRegistry.GetOrAdd(transferDescriptor, default);
                try
                {
                    Broadcast(valueType.GetDataTypeSignature().Value,
                        valueType.Meta.DefaultDTID.Value,
                        ref transferId,
                        priority,
                        buffer,
                        0,
                        payloadLen);
                }
                finally
                {
                    _transferIdRegistry[transferDescriptor] = transferId;
                }

                return SendFramesAsync();
            }
            finally
            {
                _arrayPool.Return(buffer);
            }
        }

        ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;

        public async Task<TransferReceivedArgs> SendServiceRequest(
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

                var transferId = _transferIdRegistry.GetOrAdd(transferDescriptor, default);
                try
                {
                    RequestOrRespond(destinationNodeId,
                        valueType.GetDataTypeSignature().Value,
                        valueType.Meta.DefaultDTID.Value,
                        ref transferId,
                        priority,
                        CanardRequestResponse.CanardRequest,
                        buffer,
                        0,
                        payloadLen);
                }
                finally
                {
                    _transferIdRegistry[transferDescriptor] = transferId;
                }

                await SendFramesAsync().ConfigureAwait(false);

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

            return await ticket.WaitForResponse(ct).ConfigureAwait(false);
        }

        T GetUavcanType<T>(object value) where T : class, IUavcanType
        {
            var contract = _serializer.ContractResolver.ResolveContract(value.GetType());
            var valueType = contract.UavcanType as T;
            if (valueType == null)
                throw new ArgumentException($"Cannot resolve Uavcan type for '{value.GetType().FullName}'.", nameof(value));
            if (valueType.Meta?.DefaultDTID == null)
                throw new ArgumentException(
                    $"Uavcan type '{valueType.Meta?.FullName}' resolved for '{value.GetType().FullName}' has no data type id defined.");
            return valueType;
        }

        public Task SendServiceResponse(
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

                RequestOrRespond(destinationNodeId,
                    uavcanType.GetDataTypeSignature().Value,
                    uavcanType.Meta.DefaultDTID.Value,
                    ref transferId,
                    priority,
                    CanardRequestResponse.CanardResponse,
                    buffer,
                    0,
                    payloadLen);

                return SendFramesAsync();
            }
            finally
            {
                _arrayPool.Return(buffer);
            }
        }
    }
}