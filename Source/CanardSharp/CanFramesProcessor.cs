using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp
{
    public class CanFramesProcessor
    {
        const ulong StaleTransferCleanupIntervalUsec = 1000000U;

        LinkedList<CanardRxState> _rxStates = new LinkedList<CanardRxState>();

        /**
         * The application must implement this function and supply a pointer to it to the library during initialization.
         * The library calls this function to determine whether the transfer should be received.
         *
         * If the application returns true, the value pointed to by 'out_data_type_signature' must be initialized with the
         * correct data type signature, otherwise transfer reception will fail with CRC mismatch error. Please refer to the
         * specification for more details about data type signatures. Signature for any data type can be obtained in many
         * ways; for example, using the command line tool distributed with Libcanard (see the repository).
         */
        public delegate bool CanardShouldAcceptTransferDelegate(out ulong out_data_type_signature,  ///< Must be set by the application!
                                            uint data_type_id,              ///< Refer to the specification
                                            CanardTransferType transfer_type,   ///< Refer to CanardTransferType
                                            byte source_node_id,                ///< Source node ID or Broadcast (0)
                                            byte destination_node_id);

        CanardShouldAcceptTransferDelegate _shouldAcceptTransferDelegate;

        public CanFramesProcessor(CanardShouldAcceptTransferDelegate shouldAcceptTransferDelegate)
        {
            if (shouldAcceptTransferDelegate == null)
                throw new ArgumentNullException(nameof(shouldAcceptTransferDelegate));
            _shouldAcceptTransferDelegate = shouldAcceptTransferDelegate;
        }

        /**
         * Processes a received CAN frame with a timestamp.
         * The application will call this function when it receives a new frame from the CAN bus.
         *
         * Return value will report any errors in decoding packets.
         */
        public CanardRxTransfer HandleRxFrame(
            CanFrame frame,
            ulong timestamp_usec)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));

            CleanupStaleTransfers(timestamp_usec);


            var canIdInfo = new CanIdInfo(frame.Id);
            var transferType = canIdInfo.TransferType;
            byte destination_node_id = transferType == CanardTransferType.CanardTransferTypeBroadcast ?
                                                CanardConstants.BroadcastNodeId :
                                                canIdInfo.DestinationId;

            // TODO: This function should maintain statistics of transfer errors and such.

            var canIdFlags = frame.Id.Flags;
            if (!canIdFlags.HasFlag(CanIdFlags.EFF) ||
                canIdFlags.HasFlag(CanIdFlags.RTR) ||
                canIdFlags.HasFlag(CanIdFlags.ERR) ||
                (frame.DataLength < 1))
            {
                throw new Exception("RX_INCOMPATIBLE_PACKET");
            }

            var priority = canIdInfo.Priority;
            byte source_node_id = canIdInfo.SourceId;
            var data_type_id = canIdInfo.DataType;
            var transfer_descriptor = new TransferDescriptor(data_type_id, transferType, source_node_id, destination_node_id);

            var frameInfo = frame.GetFrameInfo();

            CanardRxState rx_state;

            if (frameInfo.IsStartOfTransfer)
            {
                if (!_shouldAcceptTransferDelegate(out var data_type_signature, data_type_id, transferType, source_node_id, destination_node_id))
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
            bool tid_timed_out = (timestamp_usec - rx_state.TimestampUsec) > CanardConstants.TransferTimeoutUsec;
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

        CanardRxState GetOrCreateRxState(TransferDescriptor transfer_descriptor)
        {
            var state = _rxStates.FirstOrDefault(x => x.TransferDescriptor == transfer_descriptor);
            if (state != null)
                return state;
            state = new CanardRxState { TransferDescriptor = transfer_descriptor };
            _rxStates.AddFirst(state);
            return state;
        }

        static short ComputeTransferIDForwardDistance(byte a, byte b)
        {
            short d = (short)(b - a);
            if (d < 0)
            {
                d = (short)(d + (short)(1U << CanardConstants.TransferIdBitLen));
            }
            return d;
        }

        ulong _previousCleanupStaleTransfersTimestamp = 0;

        /**
        * Traverses the list of transfers and removes those that were last updated more than timeout_usec microseconds ago.
        * This function must be invoked by the application periodically, about once a second.
        * Also refer to the constant CANARD_RECOMMENDED_STALE_TRANSFER_CLEANUP_INTERVAL_USEC.
        */
        void CleanupStaleTransfers(ulong current_time_usec)
        {
            if (current_time_usec - _previousCleanupStaleTransfersTimestamp < StaleTransferCleanupIntervalUsec)
                return;
            _previousCleanupStaleTransfersTimestamp = current_time_usec;

            List<LinkedListNode<CanardRxState>> toRemove = null;

            var current = _rxStates.First;
            while (current != null)
            {
                if ((current_time_usec - current.Value.TimestampUsec) > CanardConstants.TransferTimeoutUsec)
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
    }
}
