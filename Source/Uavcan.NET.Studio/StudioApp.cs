using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Windows;
using Uavcan.NET.Drivers;
using Uavcan.NET.Drivers.Slcan;
using Uavcan.NET.Studio.Engine;

namespace Uavcan.NET.Studio
{
    sealed class StudioApp : Application, IDisposable, IPartImportsSatisfiedNotification
    {
        [Import]
        MainWindow _mainWindow = null;

        [Import]
        UavcanService _uavcan = null;

        [Import]
        ICompositionService _compositionService = null;

        public StudioApp()
        {
            SetupThemes();

            ShutdownMode = ShutdownMode.OnMainWindowClose;
            Startup += StudioApp_Startup;
        }

        void StudioApp_Startup(object sender, StartupEventArgs e)
        {
            var driver = GetCanDriver();
            if (driver == null)
            {
                Shutdown();
                return;
            }
            _uavcan.AddDriver(driver);

            MainWindow.Show();

            Task.Factory.StartNew(() =>
            {
                _mainWindow.Dispatcher.Invoke(() =>
                {
                    _mainWindow.Active = true;
                });
            });
        }

        void SetupThemes()
        {
            var resDictionary = new ResourceDictionary();
            resDictionary.Source = new Uri("Style.xaml", UriKind.Relative);
            Resources.MergedDictionaries.Add(resDictionary);
        }

        ICanInterface GetCanDriver()
        {
            var wnd = new ConnectionWindow(_compositionService);
            if (wnd.ShowDialog() != true)
                return null;

            var vm = wnd.ViewModel;
            return vm.Interface.Open((int)vm.BitRate);
        }

        public void Dispose()
        {
        }

        public void OnImportsSatisfied()
        {
            MainWindow = _mainWindow;
        }
    }
}
