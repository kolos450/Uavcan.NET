using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET
{
    static class PortIdRanges
    {
        public static bool IsValidRegulatedSubjectId(int id, bool isStandardType)
        {
            if (isStandardType)
                return id >= 31744 && id <= 32767;
            else
                return id >= 28672 && id <= 29695;
        }

        public static bool IsValidRegulatedServiceId(int id, bool isStandardType)
        {
            if (isStandardType)
                return id >= 384 && id <= 511;
            else
                return id >= 256 && id <= 319;
        }
    }
}
