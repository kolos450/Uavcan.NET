using System;
using System.Collections.Generic;
using System.Linq;

namespace CanardSharp.Dsdl.DataTypes
{
    public class CompositeDsdlType : DsdlType
    {
        public bool IsUnion { get; set; }
        public List<DsdlField> Fields { get; set; } = new List<DsdlField>();
        public List<object> Constants { get; set; } = new List<object>();
        internal Dictionary<string, object> Members { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);

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

        public override string GetNormalizedMemberDefinition() =>
            throw new NotSupportedException();

        public override ulong? GetDataTypeSignature() => null;

        public DsdlField TryGetField(string fieldName)
        {
            if (fieldName == null)
                throw new ArgumentNullException(nameof(fieldName));

            var fields = Fields;
            if (fieldName == null)
                return null;

            return fields.FirstOrDefault(x => x.Name.Equals(fieldName, StringComparison.Ordinal));
        }
    }
}
