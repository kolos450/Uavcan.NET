using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET.Dsdl.DataTypes
{
    public class DsdlConstant : DsdlField
    {
        public object Value { get; set; }

        public override string GetNormalizedMemberDefinition()
        {
            return $"{base.GetNormalizedMemberDefinition()} = {Value}";
        }
    }
}
