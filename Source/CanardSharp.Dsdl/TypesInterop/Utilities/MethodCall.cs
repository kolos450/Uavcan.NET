using System;
using System.Collections.Generic;
using System.Text;

namespace CanardSharp.Dsdl.TypesInterop.Utilities
{
    internal delegate TResult MethodCall<T, TResult>(T target, params object[] args);
}
