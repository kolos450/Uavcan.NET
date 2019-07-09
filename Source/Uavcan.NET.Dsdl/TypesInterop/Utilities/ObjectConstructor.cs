using System;
using System.Collections.Generic;
using System.Text;

namespace Uavcan.NET.Dsdl.TypesInterop.Utilities
{
    /// <summary>
    /// Represents a method that constructs an object.
    /// </summary>
    /// <typeparam name="T">The object type to create.</typeparam>
    public delegate object ObjectConstructor<T>(params object[] args);
}
