using Uavcan.NET.Dsdl.TypesInterop.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET.Dsdl.TypesInterop
{
    /// <summary>
    /// Contract details for a <see cref="System.Type"/> used by the <see cref="JsonSerializer"/>.
    /// </summary>
    public class ObjectContract : ContainerContract
    {
        /// <summary>
        /// Gets the object's properties.
        /// </summary>
        /// <value>The object's properties.</value>
        public DsdlPropertyCollection Properties { get; }

        /// <summary>
        /// Gets a collection of <see cref="JsonProperty"/> instances that define the parameters used with <see cref="JsonObjectContract.OverrideCreator"/>.
        /// </summary>
        public DsdlPropertyCollection CreatorParameters
        {
            get
            {
                if (_creatorParameters == null)
                {
                    _creatorParameters = new DsdlPropertyCollection(UnderlyingType);
                }

                return _creatorParameters;
            }
        }

        ///// <summary>
        ///// Gets or sets the function used to create the object. When set this function will override <see cref="JsonContract.DefaultCreator"/>.
        ///// This function is called with a collection of arguments which are defined by the <see cref="JsonObjectContract.CreatorParameters"/> collection.
        ///// </summary>
        ///// <value>The function used to create the object.</value>
        //public ObjectConstructor<object> OverrideCreator
        //{
        //    get => _overrideCreator;
        //    set => _overrideCreator = value;
        //}

        //internal ObjectConstructor<object> ParameterizedCreator
        //{
        //    get => _parameterizedCreator;
        //    set => _parameterizedCreator = value;
        //}

        //private ObjectConstructor<object> _overrideCreator;
        //private ObjectConstructor<object> _parameterizedCreator;
        private DsdlPropertyCollection _creatorParameters;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonObjectContract"/> class.
        /// </summary>
        /// <param name="underlyingType">The underlying type for the contract.</param>
        public ObjectContract(Type underlyingType)
            : base(underlyingType)
        {
            Properties = new DsdlPropertyCollection(UnderlyingType);
        }

        internal object GetUninitializedObject()
        {
            return FormatterServices.GetUninitializedObject(NonNullableUnderlyingType);
        }
    }
}
