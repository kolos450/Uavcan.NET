using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp
{
    public readonly struct DataTypeDescriptor
    {
        public DataTypeDescriptor(uint id, ulong signature)
        {
            ID = id;
            Signature = signature;
        }

        public readonly uint ID;
        public readonly ulong Signature;
    }
}
