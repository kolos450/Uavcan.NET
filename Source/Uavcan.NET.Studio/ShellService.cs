using System.ComponentModel.Composition;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace Uavcan.NET.Studio
{
    [Export]
    sealed class ShellService
    {
        [Import]
        ICompositionService _compositionService = null;

        public void RunApplication()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            WindowsFormsHost.EnableWindowsFormsInterop();

            using (var app = new StudioApp())
            {
                _compositionService.SatisfyImportsOnce(app);
                app.Properties["CCS"] = _compositionService;
                app.Run();
            }
        }
    }
}
