using CanardApp.Engine;
using CanardSharp;
using CanardSharp.Drivers.Slcan;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CanardApp
{
    sealed class CanardApp : Application, IDisposable
    {
        CanardInstance _CanardInstance;

        [Import]
        TypeResolvingService _typeResolvingService = null;

        public CanardApp()
        {
            MainWindow = new MainWindow();
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            Startup += CanardApp_Startup;
        }

        void CanardApp_Startup(object sender, StartupEventArgs e)
        {
            var connectionSettings = GetConnectionSettings();
            if (connectionSettings == null)
                Shutdown();

            var usbTin = new UsbTin();
            usbTin.Connect(connectionSettings.InterfaceName);
            usbTin.OpenCanChannel(connectionSettings.BitRate, UsbTinOpenMode.Active);

            _CanardInstance = new CanardInstance(usbTin, _typeResolvingService);

            ((MainWindow)MainWindow).Initialize(_CanardInstance);
            MainWindow.Show();
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
    }
}
