using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CanardApp.DataTypes.Protocol.Param
{
    /// <summary>
    /// Get or set a parameter by name or by index.
    /// </summary>
    /// <remarks>
    /// Note that access by index should only be used to retrieve the list of parameters; it is highly
    /// discouraged to use it for anything else, because persistent ordering is not guaranteed.
    /// </remarks>
    [DataContract(Name = "GetSet.Request", Namespace = "uavcan.protocol.param")]
    sealed class GetSet_Request
    {
        /// <summary>
        /// Index of the parameter starting from 0; ignored if name is nonempty.
        /// </summary>
        /// <remarks>
        /// Use index only to retrieve the list of parameters.
        /// Parameter ordering must be well defined (e.g. alphabetical, or any other stable ordering),
        /// in order for the index access to work.
        /// </remarks>
        [DataMember(Name = "index")]
        public ushort Index { get; set; }

        /// <remarks>
        /// If set - parameter will be assigned this value, then the new value will be returned.
        /// If not set - current parameter value will be returned.
        /// Refer to the definition of Value for details.
        /// </remarks>
        [DataMember(Name = "value")]
        public Value Value { get; set; }

        /// <summary>
        /// Name of the parameter; always preferred over index if nonempty.
        /// </summary>
        [DataMember(Name = "name")]
        public byte[] Name { get; set; }
    }

    [DataContract(Name = "GetSet.Response", Namespace = "uavcan.protocol.param")]
    sealed class GetSet_Response
    {
        /// <summary>
        /// Actual parameter value.
        /// </summary>
        /// <remarks>
        /// For set requests, it should contain the actual parameter value after the set request was
        /// executed. The objective is to let the client know if the value could not be updated, e.g.
        /// due to its range violation, etc.
        /// 
        /// Empty value (and/or empty name) indicates that there is no such parameter.
        /// </remarks>
        [DataMember(Name = "value")]
        public Value Value { get; set; }

        [DataMember(Name = "default_value")]
        public Value DefaultValue { get; set; }

        [DataMember(Name = "max_value")]
        public NumericValue MaxValue { get; set; }

        [DataMember(Name = "min_value")]
        public NumericValue MinValue { get; set; }

        /// <remarks>
        /// Empty name (and/or empty value) in response indicates that there is no such parameter.
        /// </remarks>
        [DataMember(Name = "name")]
        public byte[] Name { get; set; }
    }
}
