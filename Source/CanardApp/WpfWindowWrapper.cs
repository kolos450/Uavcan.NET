using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardApp
{
    sealed class WpfWindowWrapper : System.Windows.Forms.IWin32Window
    {
        public WpfWindowWrapper(System.Windows.Window wpfWindow)
        {
            Handle = new System.Windows.Interop.WindowInteropHelper(wpfWindow).Handle;
        }

        public IntPtr Handle { get; private set; }
    }
}
