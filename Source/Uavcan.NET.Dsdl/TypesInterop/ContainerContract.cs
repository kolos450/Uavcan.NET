using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET.Dsdl.TypesInterop
{
    /// <summary>
    /// Contract details for a <see cref="System.Type"/> used by the <see cref="JsonSerializer"/>.
    /// </summary>
    public class ContainerContract : IContract
    {
        private IContract _itemContract;
        private IContract _finalItemContract;

        // will be null for containers that don't have an item type (e.g. IList) or for complex objects
        internal IContract ItemContract
        {
            get => _itemContract;
            set
            {
                _itemContract = value;
                if (_itemContract != null)
                {
                    _finalItemContract = (_itemContract.UnderlyingType.IsSealed) ? _itemContract : null;
                }
                else
                {
                    _finalItemContract = null;
                }
            }
        }

        // the final (i.e. can't be inherited from like a sealed class or valuetype) item contract
        internal IContract FinalItemContract => _finalItemContract;

        /// <summary>
        /// Gets or sets a value indicating whether the collection items preserve object references.
        /// </summary>
        /// <value><c>true</c> if collection items preserve object references; otherwise, <c>false</c>.</value>
        public bool? ItemIsReference { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonContainerContract"/> class.
        /// </summary>
        /// <param name="underlyingType">The underlying type for the contract.</param>
        internal ContainerContract(Type underlyingType)
            : base(underlyingType)
        {
        }
    }
}
