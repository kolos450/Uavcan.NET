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

        readonly LinkedList<CanardRxState> _rxStates = new LinkedList<CanardRxState>();

        /// <summary>
        /// The processor calls this function to determine whether the transfer should be received.
        /// </summary>
        /// <remarks>
        /// If the function returns true, the value pointed to by <paramref name="dataTypeSignature" /> must be initialized with the
        /// correct data type signature, otherwise transfer reception will fail with CRC mismatch error. Please refer to the
        /// specification for more details about data type signatures.
        /// </remarks>
        public delegate bool CanardShouldAcceptTransferDelegate(
            out ulong dataTypeSignature,
            uint dataTypeId,
            CanardTransferType transferType,
            byte sourceNodeId,
            byte destinationNodeId);

        readonly CanardShouldAcceptTransferDelegate _shouldAcceptTransferDelegate;

        public CanFramesProcessor(CanardShouldAcceptTransferDelegate shouldAcceptTransferDelegate)
        {
            if (shouldAcceptTransferDelegate == null)
                throw new ArgumentNullException(nameof(shouldAcceptTransferDelegate));
            _shouldAcceptTransferDelegate = shouldAcceptTransferDelegate;
        }

        /// <summary>
        /// Processes a received CAN frame with a timestamp.
        /// </summary>
        /// <remarks>
        /// The application will call this function when it receives a new frame from the CAN bus.
        /// </remarks>
        public CanardRxTransfer HandleRxFrame(
            CanFrame frame,
            ulong timestampUsec)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));

            CleanupStaleTransfers(timestampUsec);

            var canIdInfo = new CanIdInfo(frame.Id);
            var transferType = canIdInfo.TransferType;
            byte destinationNodeId = transferType == CanardTransferType.CanardTransferTypeBroadcast ?
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
            byte sourceNodeId = canIdInfo.SourceId;
            var dataTypeId = canIdInfo.DataType;
            var transferDescriptor = new TransferDescriptor(dataTypeId, transferType, sourceNodeId, destinationNodeId);

            var frameInfo = frame.GetFrameInfo();

            CanardRxState rxState;

            if (frameInfo.IsStartOfTransfer)
            {
                if (!_shouldAcceptTransferDelegate(out var dataTypeSignature, dataTypeId, transferType, sourceNodeId, destinationNodeId))
                    return null;

                rxState = GetOrCreateRxState(transferDescriptor);
                rxState.DataTypeDescriptor = new DataTypeDescriptor(dataTypeId, dataTypeSignature);
            }
            else if (!TryGetRxState(transferDescriptor, out rxState))
            {
                throw new Exception($"Missed RX start for {transferDescriptor}.");
            }

            // Resolving the state flags:
            bool notInitialized = rxState.TimestampUsec == 0;
            bool tidTimedOut = (timestampUsec - rxState.TimestampUsec) > CanardConstants.TransferTimeoutUsec;
            bool firstFrame = frameInfo.IsStartOfTransfer;
            bool notPreviousTid =
                ComputeTransferIDForwardDistance(rxState.TransferId, frameInfo.TransferId) > 1;

            bool needRestart =
                    (notInitialized) ||
                    (tidTimedOut) ||
                    (firstFrame && notPreviousTid);

            if (needRestart)
            {
                rxState.TransferId = frameInfo.TransferId;
                rxState.NextToggle = false;
                rxState.Payload = null;
                if (!frameInfo.IsStartOfTransfer)
                {
                    rxState.TransferId++;
                    throw new Exception("RX_MISSED_START");
                }
            }

            if (frameInfo.IsStartOfTransfer && frameInfo.IsEndOfTransfer) // single frame transfer
            {
                var payload = new byte[frame.DataLength - 1];
                Buffer.BlockCopy(frame.Data, 0, payload, 0, payload.Length);

                rxState.TimestampUsec = timestampUsec;
                var rxTransfer = new CanardRxTransfer
                {
                    Payload = payload,
                    TimestampUsec = timestampUsec,
                    DataTypeId = dataTypeId,
                    TransferType = transferType,
                    TransferId = frameInfo.TransferId,
                    Priority = priority,
                    SourceNodeId = sourceNodeId
                };

                rxState.PrepareForNextTransfer();
                return rxTransfer;
            }

            if (frameInfo.ToggleBit != rxState.NextToggle)
                throw new Exception("RX_WRONG_TOGGLE");

            if (frameInfo.TransferId != rxState.TransferId)
                throw new Exception("RX_UNEXPECTED_TID");

            // Beginning of multi frame transfer.
            if (frameInfo.IsStartOfTransfer && !frameInfo.IsEndOfTransfer)
            {
                if (frame.DataLength <= 3)
                    throw new Exception("RX_SHORT_FRAME");

                // Take off the crc and store the payload.
                rxState.TimestampUsec = timestampUsec;
                rxState.AddPayload(frame.Data, 2, frame.DataLength - 3);

                rxState.PayloadCrc = (ushort)((frame.Data[0]) | (ushort)(frame.Data[1] << 8));
            }
            // Middle of a multi-frame transfer.
            else if (!frameInfo.IsStartOfTransfer && !frameInfo.IsEndOfTransfer)
            {
                rxState.AddPayload(frame.Data, 0, frame.DataLength - 1);
            }
            // End of a multi-frame transfer.
            else
            {
                rxState.AddPayload(frame.Data, 0, frame.DataLength - 1);

                var rxTransfer = new CanardRxTransfer
                {
                    TimestampUsec = timestampUsec,
                    Payload = rxState.Payload,
                    DataTypeId = dataTypeId,
                    TransferType = transferType,
                    TransferId = frameInfo.TransferId,
                    Priority = priority,
                    SourceNodeId = sourceNodeId
                };

                // CRC validation
                var actualCrc = rxState.CalculateCrc();

                rxState.PrepareForNextTransfer();

                if (actualCrc != rxState.PayloadCrc)
                    throw new Exception("RX_BAD_CRC");

                return rxTransfer;
            }

            rxState.NextToggle = !rxState.NextToggle;
            return null;
        }

        bool TryGetRxState(TransferDescriptor transferDescriptor, out CanardRxState rxState)
        {
            rxState = null;

            foreach (var i in _rxStates)
            {
                if (i.TransferDescriptor == transferDescriptor)
                {
                    rxState = i;
                    return true;
                }
            }

            return false;
        }

        CanardRxState GetOrCreateRxState(TransferDescriptor transferDescriptor)
        {
            var state = _rxStates.FirstOrDefault(x => x.TransferDescriptor == transferDescriptor);
            if (state != null)
                return state;
            state = new CanardRxState { TransferDescriptor = transferDescriptor };
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

        /// <summary>
        /// Traverses the list of transfers and removes those that were last updated more than <paramref name="currentTimeUsec"/> microseconds ago.
        /// This function must be invoked by the application periodically, about once a second.
        /// </summary>
        void CleanupStaleTransfers(ulong currentTimeUsec)
        {
            if (currentTimeUsec - _previousCleanupStaleTransfersTimestamp < StaleTransferCleanupIntervalUsec)
                return;
            _previousCleanupStaleTransfersTimestamp = currentTimeUsec;

            List<LinkedListNode<CanardRxState>> toRemove = null;

            var current = _rxStates.First;
            while (current != null)
            {
                if ((currentTimeUsec - current.Value.TimestampUsec) > CanardConstants.TransferTimeoutUsec)
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
