using Uavcan.NET.Dsdl;

namespace Uavcan.NET.Studio
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
