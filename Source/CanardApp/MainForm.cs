using CanardApp.Tools;
using CanardSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace CanardApp
{
    partial class MainForm : Form
    {
        public MainForm(CanardInstance canardInst)
        {
            InitializeComponent();
        }

        private void QuitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AboutBox().ShowDialog(this);
        }

        private void Button2_Click(object sender, EventArgs e)
        {

        }

        private void BusMonitorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenWpfWindow<BusMonitorWindow>();
        }

        void OpenWpfWindow<T>() where T : Window, new()
        {
            var wnd = new T();
            ElementHost.EnableModelessKeyboardInterop(wnd);
            wnd.Show();
        }
    }
}
