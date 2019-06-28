using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp.Dsdl.DataTypes
{
    class CompositeDsdlType : CompositeDsdlTypeBase
    {
        bool _isUnion;
        public override bool IsUnion => _isUnion;
        public void SetIsUnion(bool value) => _isUnion = value;


        List<DsdlField> _fields = new List<DsdlField>();
        public override IReadOnlyList<DsdlField> Fields => _fields;

        List<DsdlConstant> _constants = new List<DsdlConstant>();
        public override IReadOnlyList<DsdlConstant> Constants => _constants;

        internal Dictionary<string, object> Members { get; } = new Dictionary<string, object>(StringComparer.Ordinal);

        internal virtual void AddMember(DsdlMember member)
        {
            switch (member)
            {
                case DsdlConstant t:
                    _constants.Add(t);
                    break;
                case DsdlField t:
                    _fields.Add(t);
                    break;
                default:
                    throw new ArgumentException(nameof(member));
            }
        }

        public override int MaxBitlen
        {
            get
            {
                if (Fields.Count == 0)
                    return 0;
                return IsUnion ?
                    Fields.Max(x => x.Type.MaxBitlen) + Math.Max(1, Fields.Count - 1).GetBitLength() :
                    Fields.Sum(x => x.Type.MaxBitlen);
            }
        }

        public override int MinBitlen
        {
            get
            {
                if (Fields.Count == 0)
                    return 0;
                return IsUnion ?
                    Fields.Min(x => x.Type.MinBitlen) + Math.Max(1, Fields.Count - 1).GetBitLength() :
                    Fields.Sum(x => x.Type.MinBitlen);
            }
        }

        public override string GetNormalizedMemberDefinition()
            => throw new NotSupportedException();

        public override ulong? GetDataTypeSignature() => null;

        public override DsdlField TryGetField(string fieldName)
        {
            if (fieldName == null)
                throw new ArgumentNullException(nameof(fieldName));

            var fields = Fields;
            if (fieldName == null)
                return null;

            return fields.FirstOrDefault(x => fieldName.Equals(x.Name, StringComparison.Ordinal));
        }
    }
}
