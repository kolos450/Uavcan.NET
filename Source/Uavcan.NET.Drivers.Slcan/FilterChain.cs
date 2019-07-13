﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET.Drivers.Slcan
{
    /// <summary>
    /// Represents a CAN filter chain.
    /// </summary>
    public class FilterChain
    {
        /// <summary>
        /// Filter mask.
        /// </summary>
        readonly FilterMask _mask;

        /// <summary>
        /// Filters.
        /// </summary>
        readonly FilterValue[] _filters;

        /// <summary>
        /// Create filter chain with one mask and filters.
        /// </summary>
        /// <param name="mask">Mask</param>
        /// <param name="filters">Filters</param>
        public FilterChain(FilterMask mask, FilterValue[] filters)
        {
            _mask = mask;
            _filters = filters;
        }

        /// <summary>
        /// Get mask of this filter chain.
        /// </summary>
        public FilterMask Mask => _mask;

        /// <summary>
        /// Get filters of this filter chain.
        /// </summary>
        public FilterValue[] Filters => _filters;
    }
}