using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CanardApp
{
    public partial class ConnectionDialog : Form
    {
        public ConnectionDialog()
        {
            InitializeComponent();

            var portNames = SerialPort.GetPortNames();
            comboBox1.Items.AddRange(portNames);
            if (comboBox1.Items.Count > 0)
                comboBox1.SelectedIndex = 0;
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            var portName = comboBox1.SelectedItem as string;
            if (string.IsNullOrEmpty(portName))
                return;
            PortName = portName;

            BitRate = (int)numericUpDown1.Value;

            DialogResult = DialogResult.OK;
            Close();
        }

        public string PortName { get; set; }
        public int BitRate { get; set; }
    }
}
