using CanardApp.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
    }
}
