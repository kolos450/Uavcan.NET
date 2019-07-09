using Uavcan.NET.Testing.Framework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET.Testing
{
    [TestClass]
    public class UavcanRxStateTests
    {
        [TestMethod]
        public void UavcanRxStateTest0()
        {
            var state = new UavcanRxState();
            AddFrames(state, "dda088160000000000000100000000000000000000000000000000010000000000000000000000000000000000000000006b706c632e6d0061696e00");
            state.DataTypeDescriptor = new DataTypeDescriptor(1, 0xEE468A8121C46A9E);
            var payload = state.BuildTransferPayload();
            var expectedPayload = Hex.Decode("88160000000000010000000000000000000000000000010000000000000000000000000000000000006b706c632e6d61696e");
            Assert.IsTrue(expectedPayload.SequenceEqual(payload));
        }

        static void AddFrames(UavcanRxState state, string hex)
        {
            var framesCount = ((hex.Length / 2) + 7) / 8;
            for (int i = 0; i < framesCount; i++)
            {
                var offset = i * 8 * 2;
                var len = Math.Min(hex.Length - offset, 16);
                AddFrame(state, hex.Substring(offset, len));
            }
        }

        static void AddFrame(UavcanRxState state, string hex)
        {
            var bytes = Hex.Decode(hex);
            state.Frames.Add(new CanFrame(0, bytes, 0, bytes.Length));
        }
    }
}
