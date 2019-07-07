using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CanardSharp.Dsdl.DataTypes
{
    public class MessageType : CompositeDsdlTypeBase, IUavcanType
    {
        readonly CompositeDsdlTypeBase _underlyingCompoundType;

        public MessageType(CompositeDsdlTypeBase compoundType)
        {
            _underlyingCompoundType = compoundType;
        }

        public UavcanTypeMeta Meta { get; set; }

        public string GetNormalizedLayout()
        {
            var sb = new StringBuilder();
            sb.Append(Meta.FullName);
            sb.Append("\n");

            if (IsUnion)
                sb.Append("\n@union\n");

            sb.Append(string.Join("\n", Fields.Select(x => x.GetNormalizedMemberDefinition())));

            return sb.ToString().Trim().Replace("\n\n\n", "\n").Replace("\n\n", "\n");
        }

        public override string GetNormalizedMemberDefinition() => Meta.FullName;

        public override ulong? GetDataTypeSignature()
        {
            return SignatureUtilities.GetDataTypeSignature(GetNormalizedLayout(), Fields);
        }

        public override IReadOnlyList<DsdlField> Fields => _underlyingCompoundType.Fields;
        public override IReadOnlyList<DsdlConstant> Constants => _underlyingCompoundType.Constants;
        public override int MaxBitlen => _underlyingCompoundType.MaxBitlen;
        public override int MinBitlen => _underlyingCompoundType.MinBitlen;
        public override bool IsUnion => _underlyingCompoundType.IsUnion;
        public override DsdlField TryGetField(string fieldName) => _underlyingCompoundType.TryGetField(fieldName);

        internal CompositeDsdlTypeBase UnderlyingCompositeDsdlType => _underlyingCompoundType;

        public override string ToString()
        {
            var meta = Meta;
            if (meta == null)
                return "<?>";
            return meta.FullName;
        }
    }
}
