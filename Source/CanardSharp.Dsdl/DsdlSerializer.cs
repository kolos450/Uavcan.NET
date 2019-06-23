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

        public T Deserialize<T>(byte[] buffer, int offset, int length)
        {
            throw new NotImplementedException();
        }

        public int Serialize<T>(T value, byte[] buffer, int offset = 0)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var type = value.GetType();
            var bytesList = new List<byte>();
            var stream = new BitStreamWriter(bytesList);
            var contract = ContractResolver.ResolveContract(type);

            var writer = new DsdlSerializerWriter(this);
            writer.Serialize(stream, value, contract);
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
