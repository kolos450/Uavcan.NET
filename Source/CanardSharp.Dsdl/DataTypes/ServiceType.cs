using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CanardSharp.Dsdl.DataTypes
{
    public class ServiceType : IUavcanType
    {
        public UavcanTypeMeta Meta { get; set; }
        public CompositeDsdlTypeBase Request { get; set; }
        public CompositeDsdlTypeBase Response { get; set; }

        public string GetNormalizedLayout()
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

        public ulong? GetDataTypeSignature()
        {
            return SignatureUtilities.GetDataTypeSignature(GetNormalizedLayout(), Request.Fields.Concat(Response.Fields));
        }

        public UavcanTypeKind Kind => UavcanTypeKind.Service;

        public override string ToString()
        {
            var meta = Meta;
            if (meta == null)
                return "<?>";
            return meta.FullName;
        }
    }
}
