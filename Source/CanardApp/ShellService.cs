using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace CanardApp
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

            using (var app = new CanardApp())
            {
                _compositionService.SatisfyImportsOnce(app);
                app.Properties["CCS"] = _compositionService;
                app.Run();
            }
        }
    }
}
