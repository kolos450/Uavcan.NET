using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uavcan.NET.IO.Can;

namespace Uavcan.NET
{
    public class CanFramesProcessor
    {
        const ulong StaleTransferCleanupIntervalUsec = 1000000U;

        readonly LinkedList<UavcanRxState> _rxStates = new LinkedList<UavcanRxState>();

        /// <summary>
        /// The processor calls this function to determine whether the transfer should be received.
        /// </summary>
        /// <remarks>
        /// If the function returns true, the value pointed to by <paramref name="dataTypeSignature" /> must be initialized with the
        /// correct data type signature, otherwise transfer reception will fail with CRC mismatch error. Please refer to the
        /// specification for more details about data type signatures.
        /// </remarks>
        public delegate bool ShouldAcceptTransferDelegate(
            out ulong dataTypeSignature,
            uint dataTypeId,
            UavcanTransferType transferType,
            byte sourceNodeId,
            byte destinationNodeId);

        readonly ShouldAcceptTransferDelegate _shouldAcceptTransferDelegate;

        public CanFramesProcessor(ShouldAcceptTransferDelegate shouldAcceptTransferDelegate)
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
        public CanFramesProcessingResult HandleRxFrame(
            CanFrame frame,
            ulong timestampUsec)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));

            CleanupStaleTransfers(timestampUsec);

            var canIdInfo = new CanIdInfo(frame.Id);
            var transferType = canIdInfo.TransferType;
            byte destinationNodeId = transferType == UavcanTransferType.Broadcast ?
                                                UavcanConstants.BroadcastNodeId :
                                                canIdInfo.DestinationId;

            // TODO: This function should maintain statistics of transfer errors and such.

            var canIdFlags = frame.Id.Flags;
            if (!canIdFlags.HasFlag(CanIdFlags.EFF) ||
                canIdFlags.HasFlag(CanIdFlags.RTR) ||
                canIdFlags.HasFlag(CanIdFlags.ERR) ||
                (frame.DataLength < 1))
            {
                throw new CanFramesProcessingException("A frame with incompatible CAN ID received.", frame);
            }

            var priority = canIdInfo.Priority;
            byte sourceNodeId = canIdInfo.SourceId;
            var dataTypeId = canIdInfo.DataType;
            var transferDescriptor = new TransferDescriptor(dataTypeId, transferType, sourceNodeId, destinationNodeId);

            var frameInfo = frame.GetFrameInfo();

            UavcanRxState rxState;

            if (frameInfo.IsStartOfTransfer)
            {
                if (!_shouldAcceptTransferDelegate(out var dataTypeSignature, dataTypeId, transferType, sourceNodeId, destinationNodeId))
                    return default;

                rxState = GetOrCreateRxState(transferDescriptor);
                rxState.DataTypeDescriptor = new DataTypeDescriptor(dataTypeId, dataTypeSignature);
            }
            else if (!TryGetRxState(transferDescriptor, out rxState))
            {
                throw new CanFramesProcessingException($"Missed RX start for {transferDescriptor}.", frame);
            }

            // Resolving the state flags:
            bool notInitialized = rxState.TimestampUsec == 0;
            bool tidTimedOut = (timestampUsec - rxState.TimestampUsec) > UavcanConstants.TransferTimeoutUsec;
            bool firstFrame = frameInfo.IsStartOfTransfer;
            bool notPreviousTid = ComputeTransferIDForwardDistance(rxState.TransferId, frameInfo.TransferId) > 1;

            bool needRestart =
                    (notInitialized) ||
                    (tidTimedOut) ||
                    (firstFrame && notPreviousTid);

            if (needRestart)
            {
                rxState.Restart();
                rxState.TransferId = frameInfo.TransferId;
                if (!frameInfo.IsStartOfTransfer)
                {
                    rxState.TransferId++;
                    throw new CanFramesProcessingException("Missed RX start.", frame);
                }
            }

            if (frameInfo.IsStartOfTransfer && frameInfo.IsEndOfTransfer) // single frame transfer
            {
                var payload = new byte[frame.DataLength - 1];
                Buffer.BlockCopy(frame.Data, 0, payload, 0, payload.Length);

                rxState.TimestampUsec = timestampUsec;
                var rxTransfer = new UavcanRxTransfer
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
                return new CanFramesProcessingResult(rxTransfer, new[] { frame });
            }

            rxState.Frames.Add(frame);

            if (frameInfo.ToggleBit != rxState.NextToggle)
                throw new CanFramesProcessingException("Wrong toggle bit.", rxState.Frames);

            if (frameInfo.TransferId != rxState.TransferId)
                throw new CanFramesProcessingException("Unexpected transfer ID.", rxState.Frames);

            // Beginning of multi frame transfer.
            if (frameInfo.IsStartOfTransfer && !frameInfo.IsEndOfTransfer)
            {
                // Take off the crc and store the payload.
                rxState.TimestampUsec = timestampUsec;
            }
            // Middle of a multi-frame transfer.
            else if (!frameInfo.IsStartOfTransfer && !frameInfo.IsEndOfTransfer)
            { }
            // End of a multi-frame transfer.
            else
            {
                try
                {
                    var transferPayload = rxState.BuildTransferPayload();

                    var rxTransfer = new UavcanRxTransfer
                    {
                        TimestampUsec = timestampUsec,
                        Payload = transferPayload,
                        DataTypeId = dataTypeId,
                        TransferType = transferType,
                        TransferId = frameInfo.TransferId,
                        Priority = priority,
                        SourceNodeId = sourceNodeId
                    };

                    return new CanFramesProcessingResult(rxTransfer, rxState.Frames);
                }
                finally
                {
                    rxState.PrepareForNextTransfer();
                }
            }

            rxState.NextToggle = !rxState.NextToggle;
            return default;
        }

        bool TryGetRxState(TransferDescriptor transferDescriptor, out UavcanRxState rxState)
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

        UavcanRxState GetOrCreateRxState(TransferDescriptor transferDescriptor)
        {
            var state = _rxStates.FirstOrDefault(x => x.TransferDescriptor == transferDescriptor);
            if (state != null)
                return state;
            state = new UavcanRxState { TransferDescriptor = transferDescriptor };
            _rxStates.AddFirst(state);
            return state;
        }

        static short ComputeTransferIDForwardDistance(byte a, byte b)
        {
            short d = (short)(b - a);
            if (d < 0)
            {
                d = (short)(d + (short)(1U << UavcanConstants.TransferIdBitLen));
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

            List<LinkedListNode<UavcanRxState>> toRemove = null;

            var current = _rxStates.First;
            while (current != null)
            {
                if ((currentTimeUsec - current.Value.TimestampUsec) > UavcanConstants.TransferTimeoutUsec)
                {
                    if (toRemove == null)
                        toRemove = new List<LinkedListNode<UavcanRxState>>();
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
