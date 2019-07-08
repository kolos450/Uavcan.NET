using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp.Dsdl.DataTypes
{
    public interface IUavcanType
    {
        UavcanTypeKind Kind { get; }
        UavcanTypeMeta Meta { get; }

        /// <summary>
        /// Returns a string representation for normalized layout of the type.
        /// </summary>
        /// <remarks>
        /// A normalized layout contains no constants, etc.
        /// It is used for the data type signature compilation.
        /// </remarks>
        string GetNormalizedLayout();

        ulong? GetDataTypeSignature();
    }
}
