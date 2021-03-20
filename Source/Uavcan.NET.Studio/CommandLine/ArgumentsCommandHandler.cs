using System;
using System.Collections.Generic;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET.Studio.CommandLine
{
    sealed class ArgumentsCommandHandler : ICommandHandler
    {
        public ParseResult ParseResult { get; private set; }

        public Task<int> InvokeAsync(InvocationContext context)
        {
            ParseResult = context.ParseResult;
            return Task.FromResult(0);
        }
    }
}
