using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp.Dsdl.DataTypes
{
    public class DsdlField : DsdlMember
    {
        public DsdlType Type { get; set; }

        public virtual string GetNormalizedMemberDefinition()
        {
            switch (Type)
            {
                case VoidDsdlType t:
                    return t.GetNormalizedMemberDefinition();
                default:
                    return $"{Type.GetNormalizedMemberDefinition()} {Name}";
            }
        }

        public override string ToString()
        {
            return GetNormalizedMemberDefinition();
        }
    }
}
