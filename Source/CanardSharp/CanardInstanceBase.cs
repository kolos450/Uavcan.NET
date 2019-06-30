using CanardSharp.Collections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp
{
    /**
     * This is the core structure that keeps all of the states and allocated resources of the library instance.
     * The application should never access any of the fields directly! Instead, API functions should be used.
     */
    public abstract class CanardInstanceBase
    {
        /// <summary>
        /// Local node ID; may be zero if the node is anonymous.
        /// </summary>
        public byte node_id = Constants.CANARD_BROADCAST_NODE_ID;

        LinkedList<CanardRxState> _rxStates = new LinkedList<CanardRxState>(); ///< RX transfer states
        PriorityQueue<CanardCANFrame> _txQueue = new PriorityQueue<CanardCANFrame>();

        /**
         * The application must implement this function and supply a pointer to it to the library during initialization.
         * The library calls this function to determine whether the transfer should be received.
         *
         * If the application returns true, the value pointed to by 'out_data_type_signature' must be initialized with the
         * correct data type signature, otherwise transfer reception will fail with CRC mismatch error. Please refer to the
         * specification for more details about data type signatures. Signature for any data type can be obtained in many
         * ways; for example, using the command line tool distributed with Libcanard (see the repository).
         */
        public abstract bool CanardShouldAcceptTransfer(out ulong out_data_type_signature,  ///< Must be set by the application!
                                            uint data_type_id,              ///< Refer to the specification
                                            CanardTransferType transfer_type,   ///< Refer to CanardTransferType
                                            byte source_node_id);            ///< Source node ID or Broadcast (0)

        /**
         * Initializes a library instance.
         * Local node ID will be set to zero, i.e. the node will be anonymous.
         *
         * Typically, size of the memory pool should not be less than 1K, although it depends on the application. The
         * recommended way to detect the required pool size is to measure the peak pool usage after a stress-test. Refer to
         * the function canardGetPoolAllocatorStatistics().
         */
        public CanardInstanceBase()
        {
        }

        /// <summary>
        /// Node ID of the local node.
        /// </summary>
        /// <remarks>
        /// Getter returns zero (broadcast) if the node ID is not set, i.e. if the local node is anonymous.
        /// Node ID can be assigned only once.
        /// </remarks>
        public byte NodeID
        {
            get => node_id;

            set
            {
                if (node_id != Constants.CANARD_BROADCAST_NODE_ID)
                    throw new InvalidOperationException("Node ID can be assigned only once.");

                if ((value < Constants.CANARD_MIN_NODE_ID) || (value > Constants.CANARD_MAX_NODE_ID))
                    throw new ArgumentOutOfRangeException(nameof(value));

                node_id = value;
            }
        }

        /**
         * Sends a broadcast transfer.
         * If the node is in passive mode, only single frame transfers will be allowed (they will be transmitted as anonymous).
         *
         * For anonymous transfers, maximum data type ID is limited to 3 (see specification for details).
         *
         * Please refer to the specification for more details about data type signatures. Signature for any data type can be
         * obtained in many ways; for example, using the command line tool distributed with Libcanard (see the repository).
         *
         * Pointer to the Transfer ID should point to a persistent variable (e.g. static or heap allocated, not on the stack);
         * it will be updated by the library after every transmission. The Transfer ID value cannot be shared between
         * transfers that have different descriptors! More on this in the transport layer specification.
         *
         * Returns the number of frames enqueued, or negative error code.
         */
        public short Broadcast(ulong dataTypeSignature,   ///< See above
                        int dataTypeId,          ///< Refer to the specification
                        ref byte transferId,     ///< Pointer to a persistent variable containing the transfer ID
                        CanardPriority priority,               ///< Refer to definitions CANARD_TRANSFER_PRIORITY_*
                        byte[] payload,            ///< Transfer payload
                        int payloadOffset,
                        int payloadLen)          ///< Length of the above, in bytes
        {
            if (payload == null && payloadLen > 0)
                throw new ArgumentException(nameof(payload));
            if (priority > CanardPriority.Lowest)
                throw new ArgumentException(nameof(priority));

            uint can_id;
            ushort crc = CRC.InitialValue;

            if (NodeID == 0)
            {
                if (payloadLen > 7)
                {
                    throw new Exception("NODE_ID_NOT_SET");
                }

                const ushort DTIDMask = (ushort)(1U << Constants.ANON_MSG_DATA_TYPE_ID_BIT_LEN) - 1;

                if ((dataTypeId & DTIDMask) != dataTypeId)
                {
                    throw new ArgumentException(nameof(dataTypeId));
                }

                // anonymous transfer, random discriminator
                ushort discriminator = (ushort)((CRC.Add(CRC.InitialValue, payload, payloadOffset, payloadLen)) & 0x7FFEU);
                can_id = ((uint)priority << 24) | ((uint)discriminator << 9) |
                         ((uint)(dataTypeId & DTIDMask) << 8) | NodeID;
            }
            else
            {
                can_id = ((uint)priority << 24) | ((uint)dataTypeId << 8) | NodeID;

                if (payloadLen > 7)
                {
                    crc = CRC.AddSignature(crc, dataTypeSignature);
                    crc = CRC.Add(crc, payload, payloadOffset, payloadLen);
                }
            }

            short result = EnqueueTxFrames(can_id, transferId, crc, payload, payloadOffset, payloadLen);

            IncrementTransferID(ref transferId);

            return result;
        }

        /**
         * Sends a request or a response transfer.
         * Fails if the node is in passive mode.
         *
         * Please refer to the specification for more details about data type signatures. Signature for any data type can be
         * obtained in many ways; for example, using the command line tool distributed with Libcanard (see the repository).
         *
         * For Request transfers, the pointer to the Transfer ID should point to a persistent variable (e.g. static or heap
         * allocated, not on the stack); it will be updated by the library after every request. The Transfer ID value
         * cannot be shared between requests that have different descriptors! More on this in the transport layer
         * specification.
         *
         * For Response transfers, the pointer to the Transfer ID will be treated as const (i.e. read-only), and normally it
         * should point to the transfer_id field of the structure CanardRxTransfer.
         *
         * Returns the number of frames enqueued, or negative error code.
         */
        public short RequestOrRespond(int destinationNodeId,     ///< Node ID of the server/client
                               ulong dataTypeSignature,    ///< See above
                               int dataTypeId,            ///< Refer to the specification
                               ref byte transferId,      ///< Pointer to a persistent variable with transfer ID
                               CanardPriority priority,                ///< Refer to definitions CANARD_TRANSFER_PRIORITY_*
                               CanardRequestResponse kind,      ///< Refer to CanardRequestResponse
                               byte[] payload,             ///< Transfer payload
                               int payloadOffset,
                               int payloadLen)           ///< Length of the above, in bytes
        {
            if ((destinationNodeId < Constants.CANARD_MIN_NODE_ID) || (destinationNodeId > Constants.CANARD_MAX_NODE_ID))
                throw new ArgumentOutOfRangeException(nameof(destinationNodeId));
            if (payload == null && payloadLen > 0)
                throw new ArgumentException(nameof(payload));
            if (priority > CanardPriority.Lowest)
                throw new ArgumentException(nameof(priority));
            if (NodeID == 0)
                throw new InvalidOperationException("NODE_ID_NOT_SET");

            uint can_id = ((uint)priority << 24) | ((uint)dataTypeId << 16) |
                                    ((uint)kind << 15) | ((uint)destinationNodeId << 8) |
                                    (1U << 7) | NodeID;
            ushort crc = CRC.InitialValue;

            if (payloadLen > 7)
            {
                crc = CRC.AddSignature(crc, dataTypeSignature);
                crc = CRC.Add(crc, payload, payloadOffset, payloadLen);
            }

            short result = EnqueueTxFrames(can_id, transferId, crc, payload, payloadOffset, payloadLen);

            if (kind == CanardRequestResponse.CanardRequest)                      // Response Transfer ID must not be altered
            {
                IncrementTransferID(ref transferId);
            }

            return result;
        }

        /**
         * Returns a pointer to the top priority frame in the TX queue.
         * Returns null if the TX queue is empty.
         * The application will call this function after canardBroadcast() or canardRequestOrRespond() to transmit generated
         * frames over the CAN bus.
         */
        protected CanardCANFrame PeekTxQueue()
        {
            if (_txQueue.Count > 0)
                return _txQueue.Peek();
            return null;
        }

        /**
         * Removes the top priority frame from the TX queue.
         * The application will call this function after canardPeekTxQueue() once the obtained frame has been processed.
         * Calling canardBroadcast() or canardRequestOrRespond() between canardPeekTxQueue() and canardPopTxQueue()
         * is NOT allowed, because it may change the frame at the top of the TX queue.
         */
        protected void PopTxQueue()
        {
            _txQueue.Dequeue();
        }

        /**
         * Processes a received CAN frame with a timestamp.
         * The application will call this function when it receives a new frame from the CAN bus.
         *
         * Return value will report any errors in decoding packets.
         */
        protected CanardRxTransfer HandleRxFrame(CanardCANFrame frame, ulong timestamp_usec)
        {
            var transferType = frame.Id.TransferType;
            byte destination_node_id = transferType == CanardTransferType.CanardTransferTypeBroadcast ?
                                                Constants.CANARD_BROADCAST_NODE_ID :
                                                frame.Id.DestinationId;

            // TODO: This function should maintain statistics of transfer errors and such.

            if (!frame.Id.Flags.HasFlag(CanIdFlags.EFF) ||
                frame.Id.Flags.HasFlag(CanIdFlags.RTR) ||
                frame.Id.Flags.HasFlag(CanIdFlags.ERR) ||
                (frame.DataLength < 1))
            {
                throw new Exception("RX_INCOMPATIBLE_PACKET");
            }

            if (transferType != CanardTransferType.CanardTransferTypeBroadcast &&
                destination_node_id != NodeID)
            {
                throw new Exception("RX_WRONG_ADDRESS");
            }

            var priority = frame.Id.Priority;
            byte source_node_id = frame.Id.SourceId;
            var data_type_id = frame.Id.DataType;
            var transfer_descriptor = new TransferDescriptor(data_type_id, transferType, source_node_id, destination_node_id);

            var frameInfo = frame.GetFrameInfo();

            CanardRxState rx_state;

            if (frameInfo.IsStartOfTransfer)
            {
                if (!CanardShouldAcceptTransfer(out var data_type_signature, data_type_id, transferType, source_node_id))
                    return null;

                rx_state = GetOrCreateRxState(transfer_descriptor);
                rx_state.DataTypeDescriptor = new DataTypeDescriptor(data_type_id, data_type_signature);
            }
            else if (!TryGetRxState(transfer_descriptor, out rx_state))
            {
                throw new Exception($"Missed RX start for {transfer_descriptor}.");
            }

            // Resolving the state flags:
            bool not_initialized = rx_state.TimestampUsec == 0;
            bool tid_timed_out = (timestamp_usec - rx_state.TimestampUsec) > Constants.TRANSFER_TIMEOUT_USEC;
            bool first_frame = frameInfo.IsStartOfTransfer;
            bool not_previous_tid =
                ComputeTransferIDForwardDistance(rx_state.TransferId, frameInfo.TransferId) > 1;

            bool need_restart =
                    (not_initialized) ||
                    (tid_timed_out) ||
                    (first_frame && not_previous_tid);

            if (need_restart)
            {
                rx_state.TransferId = frameInfo.TransferId;
                rx_state.NextToggle = false;
                rx_state.Payload = null;
                if (!frameInfo.IsStartOfTransfer)
                {
                    rx_state.TransferId++;
                    throw new Exception("RX_MISSED_START");
                }
            }

            if (frameInfo.IsStartOfTransfer && frameInfo.IsEndOfTransfer) // single frame transfer
            {
                var payload = new byte[frame.DataLength - 1];
                Buffer.BlockCopy(frame.Data, 0, payload, 0, payload.Length);

                rx_state.TimestampUsec = timestamp_usec;
                var rx_transfer = new CanardRxTransfer
                {
                    Payload = payload,
                    TimestampUsec = timestamp_usec,
                    DataTypeId = data_type_id,
                    TransferType = transferType,
                    TransferId = frameInfo.TransferId,
                    Priority = priority,
                    SourceNodeId = source_node_id
                };

                rx_state.PrepareForNextTransfer();
                return rx_transfer;
            }

            if (frameInfo.ToggleBit != rx_state.NextToggle)
                throw new Exception("RX_WRONG_TOGGLE");

            if (frameInfo.TransferId != rx_state.TransferId)
                throw new Exception("RX_UNEXPECTED_TID");

            if (frameInfo.IsStartOfTransfer && !frameInfo.IsEndOfTransfer)      // Beginning of multi frame transfer
            {
                if (frame.DataLength <= 3)
                    throw new Exception("RX_SHORT_FRAME");

                // take off the crc and store the payload
                rx_state.TimestampUsec = timestamp_usec;
                rx_state.AddPayload(frame.Data, 2, frame.DataLength - 3);

                rx_state.PayloadCrc = (ushort)((frame.Data[0]) | (ushort)(frame.Data[1] << 8));
            }
            else if (!frameInfo.IsStartOfTransfer && !frameInfo.IsEndOfTransfer)    // Middle of a multi-frame transfer
            {
                rx_state.AddPayload(frame.Data, 0, frame.DataLength - 1);
            }
            else                                                                            // End of a multi-frame transfer
            {
                rx_state.AddPayload(frame.Data, 0, frame.DataLength - 1);

                var rx_transfer = new CanardRxTransfer
                {
                    TimestampUsec = timestamp_usec,
                    Payload = rx_state.Payload,
                    DataTypeId = data_type_id,
                    TransferType = transferType,
                    TransferId = frameInfo.TransferId,
                    Priority = priority,
                    SourceNodeId = source_node_id
                };

                // CRC validation
                var actualCrc = rx_state.CalculateCrc();

                // Making sure the payload is released even if the application didn't bother with it
                rx_state.PrepareForNextTransfer();

                if (actualCrc != rx_state.PayloadCrc)
                    throw new Exception("RX_BAD_CRC");

                return rx_transfer;
            }

            rx_state.NextToggle = !rx_state.NextToggle;
            return null;
        }

        bool TryGetRxState(TransferDescriptor transfer_descriptor, out CanardRxState rx_state)
        {
            rx_state = null;

            foreach (var i in _rxStates)
            {
                if (i.TransferDescriptor == transfer_descriptor)
                {
                    rx_state = i;
                    return true;
                }
            }

            return false;
        }

        /**
        * Traverses the list of transfers and removes those that were last updated more than timeout_usec microseconds ago.
        * This function must be invoked by the application periodically, about once a second.
        * Also refer to the constant CANARD_RECOMMENDED_STALE_TRANSFER_CLEANUP_INTERVAL_USEC.
        */
        public void CleanupStaleTransfers(ulong current_time_usec)
        {
            List<LinkedListNode<CanardRxState>> toRemove = null;

            var current = _rxStates.First;
            while (current != null)
            {
                if ((current_time_usec - current.Value.TimestampUsec) > Constants.TRANSFER_TIMEOUT_USEC)
                {
                    if (toRemove == null)
                        toRemove = new List<LinkedListNode<CanardRxState>>();
                    toRemove.Add(current);
                }

                current = current.Next;
            }

            if (toRemove != null)
            {
                foreach (var i in toRemove)
                {
                    _rxStates.Remove(i);
                }
            }
        }

        short EnqueueTxFrames(uint can_id,
                                                byte transfer_id,
                                                ushort crc,
                                            byte[] payload,
                                            int payload_offset,
                                        int payload_len)
        {
            Debug.Assert((can_id & (uint)CanIdFlags.Mask) == 0);            // Flags must be cleared

            if ((payload_len > 0) && (payload == null))
                throw new ArgumentException(nameof(payload));

            short result = 0;

            if (payload_len < Constants.CANARD_CAN_FRAME_MAX_DATA_LEN)                        // Single frame transfer
            {
                var frame = new CanardCANFrame
                {
                    Id = new CanId(can_id | (uint)CanIdFlags.EFF),
                    Data = new byte[payload_len],
                    DataLength = (byte)payload_len,
                };

                Buffer.BlockCopy(payload, payload_offset, frame.Data, 0, payload_len);

                _txQueue.Enqueue(frame);
                result++;
            }
            else                                                                    // Multi frame transfer
            {
                ushort data_index = 0;
                byte toggle = 0;
                byte sot_eot = 0x80;

                while (payload_len - data_index != 0)
                {
                    var frame = new CanardCANFrame
                    {
                        Id = new CanId(can_id | (uint)CanIdFlags.EFF),
                        Data = new byte[8],
                    };

                    byte i;
                    if (data_index == 0)
                    {
                        // add crc
                        frame.Data[0] = (byte)(crc);
                        frame.Data[1] = (byte)(crc >> 8);
                        i = 2;
                    }
                    else
                    {
                        i = 0;
                    }

                    for (; i < (Constants.CANARD_CAN_FRAME_MAX_DATA_LEN - 1) && data_index < payload_len; i++, data_index++)
                    {
                        frame.Data[i] = payload[data_index];
                    }
                    // tail byte
                    sot_eot = (data_index == payload_len) ? (byte)0x40 : sot_eot;

                    frame.Data[i] = (byte)(sot_eot | ((uint)toggle << 5) | (uint)(transfer_id & 31));
                    frame.DataLength = (byte)(i + 1);
                    _txQueue.Enqueue(frame);

                    result++;
                    toggle ^= 1;
                    sot_eot = 0;
                }
            }

            return result;
        }

        /**
         * Traverses the list of CanardRxState's and returns a pointer to the CanardRxState
         * with either the Id or a new one at the end
         */
        CanardRxState GetOrCreateRxState(TransferDescriptor transfer_descriptor)
        {
            var state = _rxStates.FirstOrDefault(x => x.TransferDescriptor == transfer_descriptor);
            if (state != null)
                return state;
            state = new CanardRxState { TransferDescriptor = transfer_descriptor };
            _rxStates.AddFirst(state);
            return state;
        }

        static void IncrementTransferID(ref byte transfer_id)
        {
            transfer_id++;
            if (transfer_id >= 32)
            {
                transfer_id = 0;
            }
        }

        static short ComputeTransferIDForwardDistance(byte a, byte b)
        {
            short d = (short)(b - a);
            if (d < 0)
            {
                d = (short)(d + (short)(1U << Constants.TRANSFER_ID_BIT_LEN));
            }
            return d;
        }
    };
}
