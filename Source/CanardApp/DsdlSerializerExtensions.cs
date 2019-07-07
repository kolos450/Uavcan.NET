using CanardSharp.Dsdl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardApp
{
    static class DsdlSerializerExtensions
    {
        public static byte[] Serialize<T>(this DsdlSerializer serializer, T data)
        {
            var buffer = new byte[serializer.GetMaxBufferLength<T>()];
            serializer.Serialize<T>(data, buffer);
            return buffer;
        }
    }
}
