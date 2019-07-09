using MahApps.Metro.Controls;
using System.IO.Ports;
using System.Windows;

namespace Uavcan.NET.Studio
{
    /// <summary>
    /// Interaction logic for ConnectionWindow.xaml
    /// </summary>
    public partial class ConnectionWindow : MetroWindow
    {
        public ConnectionWindow()
        {
            InitializeComponent();

            cbInterfaces.ItemsSource = SerialPort.GetPortNames();
            if (cbInterfaces.Items.Count > 0)
                cbInterfaces.SelectedIndex = 0;
        }

        public string InterfaceName { get; set; }
        public int BitRate { get; set; }

        void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var portName = cbInterfaces.SelectedItem as string;
            if (string.IsNullOrEmpty(portName))
                return;
            InterfaceName = portName;

            if (upBitRate.Value == null)
                return;
            BitRate = (int)upBitRate.Value.Value;

            DialogResult = true;
            Close();
        }
    }
}
