using System;
using System.Collections.Generic;
using System.Text;

namespace CanardSharp.Dsdl.TypesInterop.Utilities
{
    class ConvertUtils
    {
        public static bool IsConvertible(Type t)
        {
            return typeof(IConvertible).IsAssignableFrom(t);
        }
    }
}
