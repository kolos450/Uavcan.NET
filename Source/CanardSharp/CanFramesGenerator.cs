using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp
{
    public static class CanFramesGenerator
    {
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
        public static IEnumerable<CanFrame> Broadcast(ulong dataTypeSignature,   ///< See above
                        int dataTypeId,          ///< Refer to the specification
                        byte transferId,     ///< Pointer to a persistent variable containing the transfer ID
                        byte nodeId,
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

            if (nodeId == 0)
            {
                if (payloadLen > 7)
                {
                    throw new Exception("NODE_ID_NOT_SET");
                }

                const ushort DTIDMask = (ushort)(1U << CanardConstants.AnonymousMessageDataTypeIdBitLen) - 1;

                if ((dataTypeId & DTIDMask) != dataTypeId)
                {
                    throw new ArgumentException(nameof(dataTypeId));
                }

                // anonymous transfer, random discriminator
                ushort discriminator = (ushort)((CRC.Add(CRC.InitialValue, payload, payloadOffset, payloadLen)) & 0x7FFEU);
                can_id = ((uint)priority << 24) | ((uint)discriminator << 9) |
                         ((uint)(dataTypeId & DTIDMask) << 8) | nodeId;
            }
            else
            {
                can_id = ((uint)priority << 24) | ((uint)dataTypeId << 8) | nodeId;

                if (payloadLen > 7)
                {
                    crc = CRC.AddSignature(crc, dataTypeSignature);
                    crc = CRC.Add(crc, payload, payloadOffset, payloadLen);
                }
            }

            return CreateTxFrames(can_id, transferId, crc, payload, payloadOffset, payloadLen);
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
        public static IEnumerable<CanFrame> RequestOrRespond(int destinationNodeId,     ///< Node ID of the server/client
                               ulong dataTypeSignature,    ///< See above
                               int dataTypeId,            ///< Refer to the specification
                               byte transferId,
                               byte nodeId,      ///< Pointer to a persistent variable with transfer ID
                               CanardPriority priority,                ///< Refer to definitions CANARD_TRANSFER_PRIORITY_*
                               CanardRequestResponse kind,      ///< Refer to CanardRequestResponse
                               byte[] payload,             ///< Transfer payload
                               int payloadOffset,
                               int payloadLen)           ///< Length of the above, in bytes
        {
            if ((destinationNodeId < CanardConstants.MinNodeId) || (destinationNodeId > CanardConstants.MaxNodeId))
                throw new ArgumentOutOfRangeException(nameof(destinationNodeId));
            if (payload == null && payloadLen > 0)
                throw new ArgumentException(nameof(payload));
            if (priority > CanardPriority.Lowest)
                throw new ArgumentException(nameof(priority));

            uint can_id = ((uint)priority << 24) | ((uint)dataTypeId << 16) |
                                    ((uint)kind << 15) | ((uint)destinationNodeId << 8) |
                                    (1U << 7) | nodeId;
            ushort crc = CRC.InitialValue;

            if (payloadLen > 7)
            {
                crc = CRC.AddSignature(crc, dataTypeSignature);
                crc = CRC.Add(crc, payload, payloadOffset, payloadLen);
            }

            return CreateTxFrames(can_id, transferId, crc, payload, payloadOffset, payloadLen);
        }

        static IEnumerable<CanFrame> CreateTxFrames(uint can_id,
                                                byte transfer_id,
                                                ushort crc,
                                            byte[] payload,
                                            int payload_offset,
                                        int payload_len)
        {
            Debug.Assert((can_id & (uint)CanIdFlags.Mask) == 0);            // Flags must be cleared

            if ((payload_len > 0) && (payload == null))
                throw new ArgumentException(nameof(payload));

            if (payload_len < CanardConstants.CanFrameMaxDataLen)                        // Single frame transfer
            {
                var data = new byte[payload_len];
                Buffer.BlockCopy(payload, payload_offset, data, 0, payload_len);

                yield return new CanFrame(
                    new CanId(can_id | (uint)CanIdFlags.EFF),
                    data, 0, data.Length);
            }
            else                                                                    // Multi frame transfer
            {
                ushort data_index = 0;
                byte toggle = 0;
                byte sot_eot = 0x80;

                while (payload_len - data_index != 0)
                {
                    var data = new byte[8];

                    byte i;
                    if (data_index == 0)
                    {
                        // add crc
                        data[0] = (byte)(crc);
                        data[1] = (byte)(crc >> 8);
                        i = 2;
                    }
                    else
                    {
                        i = 0;
                    }

                    for (; i < (CanardConstants.CanFrameMaxDataLen - 1) && data_index < payload_len; i++, data_index++)
                    {
                        data[i] = payload[data_index];
                    }
                    // tail byte
                    sot_eot = (data_index == payload_len) ? (byte)0x40 : sot_eot;

                    data[i] = (byte)(sot_eot | ((uint)toggle << 5) | (uint)(transfer_id & 31));
                    var dataLength = (byte)(i + 1);

                    yield return new CanFrame(
                        new CanId(can_id | (uint)CanIdFlags.EFF),
                        data, 0, dataLength);

                    toggle ^= 1;
                    sot_eot = 0;
                }
            }
        }
    }
}
