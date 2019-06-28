using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp.Dsdl.Testing.Serialization
{
    static class SerializationTestEngine
    {
        static IUavcanTypeResolver UavcanTypeResolver;

        static SerializationTestEngine()
        {

        }

        public static void Test(object data, string expectedSerializedContent)
        {
            var expectedBytes = Hex.Decode(expectedSerializedContent);

            //var serializer = new DsdlSerializer()
        }
    }
}
