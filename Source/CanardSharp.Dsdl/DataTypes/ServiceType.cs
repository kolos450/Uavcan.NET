using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CanardSharp.Dsdl.DataTypes
{
    public class ServiceType : UavcanType
    {
        public CompositeDsdlType Request { get; set; }
        public CompositeDsdlType Response { get; set; }

        public override int MaxBitlen => throw new NotSupportedException();

        public override int MinBitlen => throw new NotSupportedException();

        protected override IEnumerable<DsdlField> Fields => Request.Fields.Concat(Response.Fields);

        public override string GetNormalizedLayout()
        {
            var sb = new StringBuilder();
            sb.Append(Meta.FullName);
            sb.Append("\n");

            if (Request.IsUnion)
                sb.Append("\n@union\n");
            sb.Append(string.Join("\n", Request.Fields.Select(x => x.GetNormalizedMemberDefinition())));
            sb.Append("\n---\n");
            if (Response.IsUnion)
                sb.Append("\n@union\n");
            sb.Append(string.Join("\n", Response.Fields.Select(x => x.GetNormalizedMemberDefinition())));

            return sb.ToString().Trim().Replace("\n\n\n", "\n").Replace("\n\n", "\n");
        }

        public override string GetNormalizedMemberDefinition() => Meta.FullName;
    }
}
