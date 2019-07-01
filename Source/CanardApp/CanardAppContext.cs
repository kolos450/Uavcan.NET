using CanardApp.Engine;
using CanardSharp;
using CanardSharp.Drivers.Slcan;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CanardApp
{
    class CanardAppContext : ApplicationContext
    {
        CanardInstance _CanardInstance;

        [Import]
        TypeResolvingService _typeResolvingService = null;

        public CanardAppContext()
        {
            MainForm = new ConnectionDialog();
            //MainForm = new MainForm(null);
        }

        protected override void OnMainFormClosed(object sender, EventArgs e)
        {
            if (sender is ConnectionDialog connectionDialog
                && connectionDialog.DialogResult == DialogResult.OK)
            {
                var usbTin = new UsbTin();
                usbTin.Connect(connectionDialog.PortName);
                usbTin.OpenCanChannel(connectionDialog.BitRate, UsbTinOpenMode.Active);

                _CanardInstance = new CanardInstance(usbTin, _typeResolvingService);
                MainForm = new MainForm(_CanardInstance);
                MainForm.Show();
            }
            else
            {
                base.OnMainFormClosed(sender, e);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_CanardInstance != null)
                {
                    _CanardInstance.Dispose();
                    _CanardInstance = null;
                }
            }

            base.Dispose(disposing);
        }

        //private void button1_Click(object sender, RoutedEventArgs e)
        //{
        //    Form1 form = new Form1();
        //    WindowInteropHelper wih = new WindowInteropHelper(this);
        //    wih.Owner = form.Handle;
        //    form.ShowDialog();
        //}
    }
}
