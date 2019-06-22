using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp.Drivers.Slcan
{
    public class CanMessageReceivedEventArgs : EventArgs
    {
        public CanMessage Message { get; set; }
    }
}
