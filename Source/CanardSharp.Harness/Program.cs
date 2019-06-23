using CanardSharp.Dsdl;
using CanardSharp.Dsdl.DataTypes;
using CanardSharp.Harness.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp.Harness
{
    class Program
    {
        static void Main(string[] args)
        {
            var schemeResolver = new FileSystemUavcanTypeResolver(@"C:\Sources\libuavcan\dsdl\kplc");
            var serializer = new DsdlSerializer(schemeResolver);

            var iostate = new IOStateRequest
            {
                State = new byte[3] { 1, 2, 3 },
                StateInverted = new byte[3] { 1, 2, 3 },
            };

            var maxBufferLength = serializer.GetMaxBufferLength<IOStateRequest>();
            var buffer = new byte[maxBufferLength];
            serializer.Serialize(iostate, buffer);

            var iostateDict = new Dictionary<string, object>
            {
                ["state"] = new byte[3] { 1, 2, 3 },
                ["state_inv"] = new byte[3] { 1, 2, 3 },
            };

            var dsdlType = schemeResolver.ResolveType("kplc", "IOState") as ServiceType;
            serializer.Serialize(iostateDict, dsdlType.Request, buffer, 0);
        }
    }
}
