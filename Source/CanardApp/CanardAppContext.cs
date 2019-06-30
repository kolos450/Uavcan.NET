using CanardApp.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CanardApp
{
    class CanardAppContext : ApplicationContext
    {
        CanardInstance _CanardInstance;

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
                _CanardInstance = new CanardInstance(connectionDialog.PortName, connectionDialog.BitRate);
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
    }
}
