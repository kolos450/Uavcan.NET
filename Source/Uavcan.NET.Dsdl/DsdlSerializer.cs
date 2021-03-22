using Uavcan.NET.Dsdl.DataTypes;
using Uavcan.NET.Dsdl.TypesInterop;
using Uavcan.NET.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET.Dsdl
{
    public class DsdlSerializer
    {
        internal readonly IUavcanTypeResolver SchemeResolver;
        public readonly ContractResolver ContractResolver;

        public DsdlSerializer(IUavcanTypeResolver schemeResolver)
        {
            SchemeResolver = schemeResolver;
            ContractResolver = new ContractResolver(schemeResolver);
        }

        public T Deserialize<T>(Memory<byte> memory)
        {
            return (T)Deserialize(typeof(T), memory);
        }

        public object Deserialize(Type type, Memory<byte> memory)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var contract = ContractResolver.ResolveContract(type);
            if (contract.DsdlType == null)
                throw new ArgumentException($"Cannot find DSDL scheme for provided type '{type.FullName}'.", nameof(type));

            return Deserialize(memory, contract.DsdlType, contract);
        }

        public object Deserialize(Memory<byte> memory, DsdlType dsdlScheme, IContract contract = null)
        {
            if (dsdlScheme == null)
                throw new ArgumentNullException(nameof(dsdlScheme));

            var stream = new BitStreamReader(memory);
            var reader = new DsdlSerializerReader(this);
            return reader.Deserialize(stream, dsdlScheme, contract);
        }

        public int Serialize<T>(T value, Memory<byte> memory)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return Serialize(value, typeof(T), memory);
        }

        public int Serialize(object value, Memory<byte> memory)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return Serialize(value, value.GetType(), memory);
        }

        public int Serialize(object value, Type type, Memory<byte> memory)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var contract = ContractResolver.ResolveContract(type);
            if (contract.DsdlType == null)
                throw new ArgumentException($"Cannot find DSDL scheme for provided type '{type.FullName}'.", nameof(type));

            return Serialize(value, contract.DsdlType, memory);
        }

        public int Serialize(object value, DsdlType dsdlScheme, Memory<byte> memory)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var streamWriter = new BitStreamWriter(memory);

            var writer = new DsdlSerializerWriter(this);
            writer.Serialize(streamWriter, value, dsdlScheme);

            return streamWriter.Length;
        }

        public int GetMaxBufferLength<T>()
        {
            var type = typeof(T);
            return GetMaxBufferLength(type);
        }

        public int GetMaxBufferLength(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var contract = ContractResolver.ResolveContract(type);
            var uavcanType = contract.DsdlType;
            return (uavcanType.MaxBitlen + 7) / 8;
        }
    }
}
