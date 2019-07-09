using Uavcan.NET.Dsdl.TypesInterop.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET.Dsdl.TypesInterop
{
    public class PrimitiveContract : IContract
    {
        internal PrimitiveTypeCode TypeCode { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonPrimitiveContract"/> class.
        /// </summary>
        /// <param name="underlyingType">The underlying type for the contract.</param>
        public PrimitiveContract(Type underlyingType)
            : base(underlyingType)
        {
            TypeCode = ConvertUtils.GetTypeCode(underlyingType);
            IsReadOnlyOrFixedSize = true;
        }
    }
}
