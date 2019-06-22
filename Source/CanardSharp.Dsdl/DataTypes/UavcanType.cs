using System.Collections.Generic;

namespace CanardSharp.Dsdl.DataTypes
{
    public abstract class UavcanType : DsdlType
    {
        public UavcanTypeMeta Meta { get; set; }

        protected abstract IEnumerable<DsdlField> Fields { get; }

        /// <summary>
        /// Returns a string representation for normalized layout of the type.
        /// </summary>
        /// <remarks>
        /// A normalized layout contains no constants, etc.
        /// It is used for the data type signature compilation.
        /// </remarks>
        public abstract string GetNormalizedLayout();

        ulong GetDsdlSignature()
        {
            return Signature.Compute(GetNormalizedLayout());
        }

        public override ulong? GetDataTypeSignature()
        {
            var sig = new Signature(GetDsdlSignature());
            foreach (var f in Fields)
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
