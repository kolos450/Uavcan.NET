using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uavcan.NET.IO.Can;

namespace Uavcan.NET
{
    public static class CanFramesGenerator
    {
        /// <summary>
        /// Generates frames for a broadcast transfer.
        /// </summary>
        /// <remarks>
        /// If the node is in passive mode, only single frame transfers will be allowed (they will be transmitted as anonymous).
        /// 
        /// For anonymous transfers, maximum data type ID is limited to 3 (see specification for details).
        /// 
        /// Pointer to the Transfer ID should point to a persistent variable and should be updated after every transmission.
        /// The Transfer ID value cannot be shared between transfers that have different descriptors.
        /// </remarks>
        public static IEnumerable<CanFrame> Broadcast(
            ulong dataTypeSignature,
            int dataTypeId,
            byte transferId,
            byte nodeId,
            UavcanPriority priority,
            byte[] payload,
            int payloadOffset,
            int payloadLen)
        {
            if (payload == null && payloadLen > 0)
                throw new ArgumentException(nameof(payload));
            if (priority > UavcanPriority.Lowest)
                throw new ArgumentException(nameof(priority));

            uint canId;
            ushort crc = Crc16.InitialValue;

            if (nodeId == 0)
            {
                if (payloadLen > 7)
                {
                    throw new ArgumentException("Node ID expected.", nameof(nodeId));
                }

                const ushort DTIDMask = (ushort)(1U << UavcanConstants.AnonymousMessageDataTypeIdBitLen) - 1;

                if ((dataTypeId & DTIDMask) != dataTypeId)
                {
                    throw new ArgumentException(nameof(dataTypeId));
                }

                // Anonymous transfer, random discriminator.
                ushort discriminator = (ushort)((Crc16.Add(Crc16.InitialValue, payload, payloadOffset, payloadLen)) & 0x7FFEU);
                canId = ((uint)priority << 24) | ((uint)discriminator << 9) |
                         ((uint)(dataTypeId & DTIDMask) << 8) | nodeId;
            }
            else
            {
                canId = ((uint)priority << 24) | ((uint)dataTypeId << 8) | nodeId;

                if (payloadLen > 7)
                {
                    crc = Crc16.AddSignature(crc, dataTypeSignature);
                    crc = Crc16.Add(crc, payload, payloadOffset, payloadLen);
                }
            }

            return CreateTxFrames(canId, transferId, crc, payload, payloadOffset, payloadLen);
        }

        /// <summary>
        /// Generates frames for a request or a response transfer.
        /// </summary>
        /// <remarks>
        /// Fails if the node is in passive mode.
        /// 
        /// Pointer to the Transfer ID should point to a persistent variable and should be updated after every transmission.
        /// The Transfer ID value cannot be shared between transfers that have different descriptors.
        /// </remarks>
        public static IEnumerable<CanFrame> RequestOrRespond(
            int destinationNodeId,
            ulong dataTypeSignature,
            int dataTypeId,
            byte transferId,
            byte nodeId,
            UavcanPriority priority,
            UavcanRequestResponse kind,
            byte[] payload,
            int payloadOffset,
            int payloadLen)
        {
            if ((destinationNodeId < UavcanConstants.MinNodeId) || (destinationNodeId > UavcanConstants.MaxNodeId))
                throw new ArgumentOutOfRangeException(nameof(destinationNodeId));
            if (payload == null && payloadLen > 0)
                throw new ArgumentException(nameof(payload));
            if (priority > UavcanPriority.Lowest)
                throw new ArgumentException(nameof(priority));
            if (nodeId == UavcanConstants.BroadcastNodeId)
                throw new ArgumentException("Anonymous node can send broadcast messages only.", nameof(nodeId));

            uint canId = ((uint)priority << 24) | ((uint)dataTypeId << 16) |
                                    ((uint)kind << 15) | ((uint)destinationNodeId << 8) |
                                    (1U << 7) | nodeId;
            ushort crc = Crc16.InitialValue;

            if (payloadLen > 7)
            {
                crc = Crc16.AddSignature(crc, dataTypeSignature);
                crc = Crc16.Add(crc, payload, payloadOffset, payloadLen);
            }

            return CreateTxFrames(canId, transferId, crc, payload, payloadOffset, payloadLen);
        }

        static IEnumerable<CanFrame> CreateTxFrames(
            uint canId,
            byte transferId,
            ushort crc,
            byte[] payload,
            int payloadOffset,
            int payloadLen)
        {
            if ((canId & (uint)CanIdFlags.Mask) != 0)
                throw new ArgumentException("Flags must be cleared.", nameof(canId));

            if ((payloadLen > 0) && (payload == null))
                throw new ArgumentException(nameof(payload));

            // Single frame transfer.
            if (payloadLen < UavcanConstants.CanFrameMaxDataLen)
            {
                var data = new byte[payloadLen + 1];
                Buffer.BlockCopy(payload, payloadOffset, data, 0, payloadLen);

                data[data.Length - 1] = (byte)(0xC0U | (transferId & 31));

                yield return new CanFrame(
                    new CanId(canId | (uint)CanIdFlags.EFF),
                    data, 0, data.Length);
            }
            // Multi frame transfer.
            else
            {
                ushort dataIndex = 0;
                byte toggle = 0;
                byte sotEot = 0x80;

                while (payloadLen - dataIndex != 0)
                {
                    var data = new byte[8];

                    byte i;
                    if (dataIndex == 0)
                    {
                        // Add CRC
                        data[0] = (byte)(crc);
                        data[1] = (byte)(crc >> 8);
                        i = 2;
                    }
                    else
                    {
                        i = 0;
                    }

                    for (; i < (UavcanConstants.CanFrameMaxDataLen - 1) && dataIndex < payloadLen; i++, dataIndex++)
                    {
                        data[i] = payload[dataIndex];
                    }
                    // Tail byte
                    sotEot = (dataIndex == payloadLen) ? (byte)0x40 : sotEot;

                    data[i] = (byte)(sotEot | ((uint)toggle << 5) | (uint)(transferId & 31));
                    var dataLength = (byte)(i + 1);

                    yield return new CanFrame(
                        new CanId(canId | (uint)CanIdFlags.EFF),
                        data, 0, dataLength);

                    toggle ^= 1;
                    sotEot = 0;
                }
            }
        }
    }
}
