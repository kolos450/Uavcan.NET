using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Windows;
using Uavcan.NET.IO.Can.Drivers;

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
            _mainWindow.Active = true;
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

            return wnd.ViewModel.Driver;
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
