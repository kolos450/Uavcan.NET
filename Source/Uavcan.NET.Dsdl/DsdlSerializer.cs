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

        public T Deserialize<T>(byte[] buffer, int offset, int length)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            return (T)Deserialize(typeof(T), buffer, offset, length);
        }

        public object Deserialize(Type type, byte[] buffer, int offset, int length)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            var contract = ContractResolver.ResolveContract(type);
            if (contract.DsdlType == null)
                throw new ArgumentException($"Cannot find DSDL scheme for provided type '{type.FullName}'.", nameof(type));

            return Deserialize(buffer, offset, length, contract.DsdlType, contract);
        }

        public object Deserialize(byte[] buffer, int offset, int length, DsdlType dsdlScheme, IContract contract = null)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (dsdlScheme == null)
                throw new ArgumentNullException(nameof(dsdlScheme));

            var stream = new BitStreamReader(buffer, offset, length);
            var reader = new DsdlSerializerReader(this);
            return reader.Deserialize(stream, dsdlScheme, contract);
        }

        public int Serialize<T>(T value, byte[] buffer, int offset = 0)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            return Serialize(value, typeof(T), buffer, offset);
        }

        public int Serialize(object value, byte[] buffer, int offset = 0)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            return Serialize(value, value.GetType(), buffer, offset);
        }

        public int Serialize(object value, Type type, byte[] buffer, int offset = 0)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            var contract = ContractResolver.ResolveContract(type);
            if (contract.DsdlType == null)
                throw new ArgumentException($"Cannot find DSDL scheme for provided type '{type.FullName}'.", nameof(type));

            return Serialize(value, contract.DsdlType, buffer, offset);
        }

        public int Serialize(object value, DsdlType dsdlScheme, byte[] buffer, int offset = 0)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            var bytesList = new List<byte>();
            var stream = new BitStreamWriter(bytesList);

            var writer = new DsdlSerializerWriter(this);
            writer.Serialize(stream, value, dsdlScheme);
            var bytesCount = bytesList.Count;

            for (int i = 0; i < bytesCount; i++)
            {
                buffer[offset + i] = bytesList[i];
            }

            return bytesCount;
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
