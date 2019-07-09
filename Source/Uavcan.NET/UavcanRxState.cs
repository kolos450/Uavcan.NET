using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Uavcan.NET
{
    public class UavcanRxState
    {
        public ulong TimestampUsec;

        public TransferDescriptor TransferDescriptor;

        public byte TransferId;
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

        static readonly byte[] _emptyPayload = new byte[0];

        public byte[] BuildTransferPayload()
        {
            if (Frames.Count == 0)
                return _emptyPayload;

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
                            throw new CanFramesProcessingException("RX_SHORT_FRAME", Frames);
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
                throw new CanFramesProcessingException("RX_BAD_CRC", Frames);

            return transferPayload;
        }
    };
}
