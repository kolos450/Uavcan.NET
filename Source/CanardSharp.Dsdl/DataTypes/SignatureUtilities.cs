using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp.Dsdl.DataTypes
{
    static class SignatureUtilities
    {
        public static ulong GetDataTypeSignature(string normalizedLayout, IEnumerable<DsdlField> fields)
        {
            var layoutSignature = Signature.Compute(normalizedLayout);
            var sig = new Signature(layoutSignature);
            foreach (var f in fields)
            {
                var fieldSig = f.Type.GetDataTypeSignature();
                if (fieldSig != null)
                {
                    var sigValue = sig.Value;
                    sig.Add(Signature.bytes_from_crc64(fieldSig.Value));
                    sig.Add(Signature.bytes_from_crc64(sigValue));
                }
            }
            return sig.Value;
        }
    }
}
