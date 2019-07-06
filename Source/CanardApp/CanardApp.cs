using CanardApp.Engine;
using CanardSharp;
using CanardSharp.Drivers.Slcan;
using MahApps.Metro;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CanardApp
{
    sealed class CanardApp : Application, IDisposable, IPartImportsSatisfiedNotification
    {
        CanardInstance _CanardInstance;

        [Import]
        TypeResolvingService _typeResolvingService = null;

        [Import]
        MainWindow _mainWindow = null;

        public CanardApp()
        {
            SetupThemes();

            ShutdownMode = ShutdownMode.OnMainWindowClose;
            Startup += CanardApp_Startup;
        }

        void CanardApp_Startup(object sender, StartupEventArgs e)
        {

            var connectionSettings = GetConnectionSettings();
            if (connectionSettings == null)
                Shutdown();

            MainWindow.Show();

            Task.Factory.StartNew(() =>
            {
                var usbTin = new UsbTin();
                usbTin.Connect(connectionSettings.InterfaceName);
                usbTin.OpenCanChannel(connectionSettings.BitRate, UsbTinOpenMode.Active);

                _CanardInstance = new CanardInstance(usbTin, _typeResolvingService);

                _mainWindow.Dispatcher.Invoke(() =>
                {
                    ((MainWindow)MainWindow).Initialize(_CanardInstance);
                });
            });
        }

        void SetupThemes()
        {
            var resDictionary = new ResourceDictionary();

            resDictionary.Source = new Uri("Style.xaml", UriKind.Relative);
            Current.Resources.MergedDictionaries.Add(resDictionary);
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
            if (_CanardInstance != null)
            {
                _CanardInstance.Dispose();
                _CanardInstance = null;
            }
        }

        public void OnImportsSatisfied()
        {
            MainWindow = _mainWindow;
        }
    }
}
