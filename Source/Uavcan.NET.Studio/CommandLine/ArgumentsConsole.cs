using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.IO;
using System.Text;

namespace Uavcan.NET.Studio.CommandLine
{
    sealed class ArgumentsConsole : IConsole
    {
        public bool IsOutputRedirected => true;
        public bool IsErrorRedirected => true;
        public bool IsInputRedirected => true;

        IStandardStreamWriter IStandardOut.Out => Out;
        IStandardStreamWriter IStandardError.Error => Out;

        public RecordingWriter Out { get; } = new RecordingWriter();
    }
}
