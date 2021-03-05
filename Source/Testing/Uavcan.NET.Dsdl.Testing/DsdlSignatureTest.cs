using System;
using Xunit;

namespace Uavcan.NET.Dsdl.Testing
{
    public class DsdlSignatureTest
    {
        static UavcanTypeMeta Meta(string fullName)
        {
            var dotIndex = fullName.LastIndexOf('.');
            return new UavcanTypeMeta
            {
                Name = fullName.Substring(dotIndex + 1),
                Namespace = fullName.Substring(0, dotIndex)
            };
        }

        [Fact]
        public void TestDsdlSignatureEmptyMessage()
        {
            var resolver = new StringUavcanTypeResolver(new[]
            {
                (Meta("uavcan.protocol.param.Empty"), "")
            });
            var type = resolver.ResolveType(string.Empty, "uavcan.protocol.param.Empty");

            Assert.Equal(0x6C4D0E8EF37361DFUL, type.GetDataTypeSignature());
        }

        [Fact]
        public void TestDsdlSignatureSimpleMessage()
        {
            var resolver = new StringUavcanTypeResolver(new[]
            {
                (Meta("uavcan.protocol.debug.LogLevel"), @"uint3 DEBUG    = 0
uint3 INFO     = 1
uint3 WARNING  = 2
uint3 ERROR    = 3
uint3 value")
            });
            var type = resolver.ResolveType(string.Empty, "uavcan.protocol.debug.LogLevel");

            Assert.Equal(0x711BF141AF572346UL, type.GetDataTypeSignature());
        }

        [Fact]
        public void TestDsdlSignatureCompoundMessage()
        {
            var resolver = new StringUavcanTypeResolver(new[]
            {
                (Meta("uavcan.protocol.debug.LogLevel"), @"uint3 DEBUG    = 0
uint3 INFO     = 1
uint3 WARNING  = 2
uint3 ERROR    = 3
uint3 value"),
                (Meta("uavcan.protocol.debug.LogMessage"), @"LogLevel level
uint8[<=31] source
uint8[<=90] text"),
            });
            var type = resolver.ResolveType(string.Empty, "uavcan.protocol.debug.LogMessage");

            Assert.Equal(0xD654A48E0C049D75UL, type.GetDataTypeSignature());
        }

        [Fact]
        public void TestDsdlSignatureSimpleService()
        {
            var resolver = new StringUavcanTypeResolver(new[]
            {
                (Meta("uavcan.protocol.param.ExecuteOpcode"), @"uint8 OPCODE_SAVE  = 0
uint8 OPCODE_ERASE = 1
uint8 opcode
int48 argument
---
int48 argument
bool ok")
            });
            var type = resolver.ResolveType(string.Empty, "uavcan.protocol.param.ExecuteOpcode");

            Assert.Equal(0x3B131AC5EB69D2CDUL, type.GetDataTypeSignature());
        }

        [Fact]
        public void TestDsdlSignatureCompoundService()
        {
            var resolver = new StringUavcanTypeResolver(new[]
            {
                (Meta("uavcan.protocol.param.Empty"), @""),
                (Meta("uavcan.protocol.param.NumericValue"), @"@union
Empty empty
int64   integer_value
float32 real_value"),
                (Meta("uavcan.protocol.param.Value"), @"@union
Empty empty
int64        integer_value
float32      real_value
uint8        boolean_value
uint8[<=128] string_value"),
                (Meta("uavcan.protocol.param.GetSet"), @"uint13 index
Value value
uint8[<=92] name
---
void5
Value value
void5
Value default_value
void6
NumericValue max_value
void6
NumericValue min_value
uint8[<=92] name"),
            });
            var type = resolver.ResolveType(string.Empty, "uavcan.protocol.param.GetSet");

            Assert.Equal(0xA7B622F939D1A4D5UL, type.GetDataTypeSignature());
        }
    }
}
