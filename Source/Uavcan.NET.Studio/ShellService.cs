using ReactiveUI;
using Splat;
using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Reactive;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace Uavcan.NET.Studio
{
    [Export]
    sealed class ShellService
    {
        [Import]
        CompositionContainer _compositionContainer = null;

        public void RunApplication()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            WindowsFormsHost.EnableWindowsFormsInterop();

            using (var app = new StudioApp())
            {
                app.Properties["CCS"] = _compositionContainer;
                _compositionContainer.SatisfyImportsOnce(app);

                InitializeRxUI(_compositionContainer);

                app.Run();
            }
        }

        static void InitializeRxUI(CompositionContainer container)
        {
            RxApp.DefaultExceptionHandler = Observer.Create<Exception>(
                ex => { if (!(ex is OperationCanceledException)) Logger.Log(ex); },
                ex => { if (!(ex is OperationCanceledException)) Logger.Log(ex); });

            var mefResolver = new MefDependencyResolver(container);
            mefResolver.InitializeSplat();
            mefResolver.InitializeReactiveUI();
            Locator.SetLocator(mefResolver);
        }

        public static void SatisfyImportsOnce(object attributedPart)
        {
            ((ICompositionService)System.Windows.Application.Current.Properties["CCS"]).SatisfyImportsOnce(attributedPart);
        }
    }
}
