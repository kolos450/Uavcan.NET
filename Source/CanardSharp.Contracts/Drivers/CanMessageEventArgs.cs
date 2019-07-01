using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp.Drivers
{
    public class CanMessageEventArgs : EventArgs
    {
        public CanFrame Message { get; set; }
    }
}
