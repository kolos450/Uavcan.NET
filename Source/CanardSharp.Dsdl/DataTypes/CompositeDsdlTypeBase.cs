using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CanardSharp.Dsdl.DataTypes
{
    public abstract class CompositeDsdlTypeBase : DsdlType
    {
        public abstract bool IsUnion { get; }

        public abstract IReadOnlyList<DsdlField> Fields { get; }

        public abstract IReadOnlyList<DsdlConstant> Constants { get; }

        public override string GetNormalizedMemberDefinition()
            => throw new NotSupportedException();

        public override ulong? GetDataTypeSignature() => null;

        public abstract DsdlField TryGetField(string fieldName);
    }
}
