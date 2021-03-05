using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Uavcan.NET.IO.Can;

namespace Uavcan.NET
{
    public class UavcanRxState
    {
        public ulong TimestampUsec;

        public TransferDescriptor TransferDescriptor;

        byte _transferId;
        public byte TransferId
        {
            get => _transferId;
            set => _transferId = (byte)(value & 0x1F);
        }

        public bool NextToggle;

        public void PrepareForNextTransfer()
        {
            Frames = new List<CanFrame>();
            TransferId++;
            NextToggle = false;
        }

        public void Restart()
        {
            Frames = new List<CanFrame>();
            NextToggle = false;
        }

        public DataTypeDescriptor DataTypeDescriptor { get; set; }
        public List<CanFrame> Frames { get; private set; } = new List<CanFrame>();

        ushort CalculateCrc(byte[] data)
        {
            var crc = Crc16.AddSignature(Crc16.InitialValue, DataTypeDescriptor.Signature);
            return Crc16.Add(crc, data, 0, data.Length);
        }

        public byte[] BuildTransferPayload()
        {
            if (Frames.Count == 0)
                return Array.Empty<byte>();

            ushort expectedCrc = 0;
            byte[] transferPayload;
            using (var ms = new MemoryStream())
            {
                for (int i = 0; i < Frames.Count; i++)
                {
                    var currentFrame = Frames[i];
                    if (i == 0)
                    {
                        if (currentFrame.DataLength <= 3)
                            throw new CanFramesProcessingException("Frame too short.", Frames);
                        expectedCrc = (ushort)((currentFrame.Data[0]) | (ushort)(currentFrame.Data[1] << 8));
                        ms.Write(currentFrame.Data, 2, currentFrame.DataLength - 3);
                    }
                    else
                    {
                        ms.Write(currentFrame.Data, 0, currentFrame.DataLength - 1);
                    }
                }

                transferPayload = ms.ToArray();
            }

            var actualCrc = CalculateCrc(transferPayload);
            if (actualCrc != expectedCrc)
                throw new CanFramesProcessingException("Bad CRC.", Frames);

            return transferPayload;
        }
    };
}
