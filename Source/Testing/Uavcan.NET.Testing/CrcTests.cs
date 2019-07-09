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
    public class CrcTests
    {
        [TestMethod]
        public void Crc16Test0()
        {
            var payload = Hex.Decode("a9eed90c0000000000010000000000000000000000000000010000000000000000000000000000000000006b706c632e6d61696e");
            ushort expectedCrc = 0xEEA9;
            ushort dataTypeSignature = 0xD9A7;

            RunCrcTest(payload, dataTypeSignature, expectedCrc);
        }

        void RunCrcTest(byte[] payload, ushort dataTypeSignature, ushort expectedCrc)
        {
            var payloadCrc = payload[0] | (payload[1] << 8);
            Assert.AreEqual(expectedCrc, payloadCrc);

            var actualCrc = Crc16.Add(dataTypeSignature, payload, 2, payload.Length - 2);

            Assert.AreEqual(expectedCrc, actualCrc);
        }

        [TestMethod]
        public void Crc16Test1()
        {
            var dtCrc = Crc16.AddSignature(Crc16.InitialValue, 0xEE468A8121C46A9E);
            Assert.AreEqual(dtCrc, 0xD9A7);
        }
    }
}
