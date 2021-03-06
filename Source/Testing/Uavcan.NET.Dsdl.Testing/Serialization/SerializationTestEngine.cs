﻿using Uavcan.NET.Testing.Framework;
using Irony.Parsing;
using KellermanSoftware.CompareNetObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Uavcan.NET.Dsdl.Testing.Serialization
{
    static class SerializationTestEngine
    {
        static readonly IUavcanTypeResolver _uavcanTypeResolver;
        static readonly CompareLogic _compareLogic = new CompareLogic();

        static SerializationTestEngine()
        {
            var assembly = typeof(SerializationTestEngine).Assembly;
            string scheme;

            Stream stream = null;
            try
            {
                stream = assembly.GetManifestResourceStream(typeof(SerializationTestEngine), "AutogeneratedTests.Scheme.txt");
                using (var reader = new StreamReader(stream))
                {
                    stream = null;
                    scheme = reader.ReadToEnd();
                }
            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }

            _uavcanTypeResolver = new TypeResolverFactory().Create(scheme);
        }

        public static void Test(object data, string expectedSerializedContent, bool doRoundtripTest)
        {
            var expectedBytes = Hex.Decode(expectedSerializedContent);

            var serializer = new DsdlSerializer(_uavcanTypeResolver);

            var bufferLength = serializer.GetMaxBufferLength(data.GetType());
            var buffer = new byte[bufferLength];
            var size = serializer.Serialize(data, buffer);
            buffer = buffer.Take(size).ToArray();

            Assert.True(expectedBytes.SequenceEqual(buffer), "Serialized payload mismatch.");

            var deserialized = serializer.Deserialize(data.GetType(), buffer);

            if (doRoundtripTest)
            {
                var comparisonResult = _compareLogic.Compare(data, deserialized);
                if (!comparisonResult.AreEqual)
                {
                    bool ignoreInequality = true;
                    foreach (var diff in comparisonResult.Differences)
                    {
                        if (!ignoreInequality)
                            break;

                        if (diff.Object1 is float f1 && diff.Object2 is float f2)
                        {
                            var fdiff = Math.Abs(f1 - f2);
                            if (fdiff > 0.0001)
                                ignoreInequality = false;
                        }
                        else
                        {
                            ignoreInequality = false;
                        }
                    }

                    Assert.True(ignoreInequality, comparisonResult.DifferencesString);
                }
            }
        }
    }
}
