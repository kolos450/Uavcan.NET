using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CanardSharp.Dsdl.DataTypes
{
    public class MessageType : UavcanType
    {
        public CompositeDsdlType Message { get; set; }

        public override int MaxBitlen => Message.MaxBitlen;
        public override int MinBitlen => Message.MinBitlen;

        protected override IEnumerable<DsdlField> Fields => Message.Fields;

        public override string GetNormalizedLayout()
        {
            var sb = new StringBuilder();
            sb.Append(Meta.FullName);
            sb.Append("\n");

            if (Message.IsUnion)
                sb.Append("\n@union\n");

            sb.Append(string.Join("\n", Message.Fields.Select(x => x.GetNormalizedMemberDefinition())));

            return sb.ToString().Trim().Replace("\n\n\n", "\n").Replace("\n\n", "\n");
        }

        public override string GetNormalizedMemberDefinition() => Meta.FullName;
    }
}
