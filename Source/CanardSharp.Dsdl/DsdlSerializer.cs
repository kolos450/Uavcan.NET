using CanardSharp.Dsdl.DataTypes;
using CanardSharp.Dsdl.TypesInterop;
using CanardSharp.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp.Dsdl
{
    public class DsdlSerializer
    {
        internal readonly IUavcanTypeResolver SchemeResolver;
        internal readonly ContractResolver ContractResolver;

        public DsdlSerializer(IUavcanTypeResolver schemeResolver)
        {
            SchemeResolver = schemeResolver;
            ContractResolver = new ContractResolver(schemeResolver);
        }

        public T Deserialize<T>(byte[] buffer, int offset)
        {
            var type = typeof(T);
            var contract = ContractResolver.ResolveContract(type);
            if (contract.DsdlType == null)
                throw new ArgumentException("Cannot find DSDL scheme for provided type.", nameof(T));

            return (T)Deserialize(buffer, offset, contract.DsdlType, contract);
        }

        public object Deserialize(byte[] buffer, int offset, DsdlType dsdlScheme, IContract contract = null)
        {
            var stream = new BitStreamReader(buffer, startIndex: offset);
            var reader = new DsdlSerializerReader(this);
            return reader.Deserialize(stream, dsdlScheme, contract);
        }

        public int Serialize<T>(T value, byte[] buffer, int offset = 0)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var type = typeof(T);
            var contract = ContractResolver.ResolveContract(type);
            if (contract.DsdlType == null)
                throw new ArgumentException("Cannot find DSDL scheme for provided type.", nameof(T));

            return Serialize(value, contract.DsdlType, buffer, offset);
        }

        public int Serialize(object value, DsdlType dsdlScheme, byte[] buffer, int offset = 0)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

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
            var contract = ContractResolver.ResolveContract(type);
            var uavcanType = contract.DsdlType;
            return (uavcanType.MaxBitlen + 7) / 8;
        }
    }
}
