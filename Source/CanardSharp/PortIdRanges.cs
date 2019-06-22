using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp
{
    static class PortIdRanges
    {
        public static bool is_valid_regulated_subject_id(int id, bool isStandardType)
        {
            if (isStandardType)
                return id >= 31744 && id <= 32767;
            else
                return id >= 28672 && id <= 29695;
        }

        public static bool is_valid_regulated_service_id(int id, bool isStandardType)
        {
            if (isStandardType)
                return id >= 384 && id <= 511;
            else
                return id >= 256 && id <= 319;
        }
    }
}
