using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET.IO.Can.Drivers.Slcan
{
    /// <summary>
    /// Represents a CAN filter chain.
    /// </summary>
    public class FilterChain
    {
        /// <summary>
        /// Create filter chain with one mask and filters.
        /// </summary>
        /// <param name="mask">Mask</param>
        /// <param name="filters">Filters</param>
        public FilterChain(FilterMask mask, FilterValue[] filters)
        {
            Mask = mask;
            Filters = filters;
        }

        /// <summary>
        /// Get mask of this filter chain.
        /// </summary>
        public FilterMask Mask { get; }

        /// <summary>
        /// Get filters of this filter chain.
        /// </summary>
        public FilterValue[] Filters { get; }
    }
}
