using System;

namespace Uavcan.NET.Studio
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
