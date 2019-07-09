using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Windows;
using Uavcan.NET.Drivers.Slcan;
using Uavcan.NET.Studio.Engine;

namespace Uavcan.NET.Studio
{
    sealed class StudioApp : Application, IDisposable, IPartImportsSatisfiedNotification
    {
        UavcanInstance _uavcan;

        [Import]
        TypeResolvingService _typeResolvingService = null;

        [Import]
        MainWindow _mainWindow = null;

        public StudioApp()
        {
            SetupThemes();

            ShutdownMode = ShutdownMode.OnMainWindowClose;
            Startup += StudioApp_Startup;
        }

        void StudioApp_Startup(object sender, StartupEventArgs e)
        {
            var connectionSettings = GetConnectionSettings();
            if (connectionSettings == null)
            {
                Shutdown();
                return;
            }

            MainWindow.Show();

            Task.Factory.StartNew(() =>
            {
                var usbTin = new UsbTin();
                usbTin.Connect(connectionSettings.InterfaceName);
                usbTin.OpenCanChannel(connectionSettings.BitRate, UsbTinOpenMode.Active);

                _uavcan = new UavcanInstance(usbTin, _typeResolvingService);

                _mainWindow.Dispatcher.Invoke(() =>
                {
                    ((MainWindow)MainWindow).Initialize(_uavcan);
                });
            });
        }

        void SetupThemes()
        {
            var resDictionary = new ResourceDictionary();
            resDictionary.Source = new Uri("Style.xaml", UriKind.Relative);
            Resources.MergedDictionaries.Add(resDictionary);
        }

        static ConnectionSettings GetConnectionSettings()
        {
            var wnd = new ConnectionWindow();
            if (wnd.ShowDialog() != true)
                return null;

            return new ConnectionSettings
            {
                InterfaceName = wnd.InterfaceName,
                BitRate = wnd.BitRate,
            };
        }

        public void Dispose()
        {
            if (MainWindow is IDisposable disposable)
            {
                disposable.Dispose();
            }

            if (_uavcan != null)
            {
                _uavcan.Dispose();
                _uavcan = null;
            }
        }

        public void OnImportsSatisfied()
        {
            MainWindow = _mainWindow;
        }
    }
}
