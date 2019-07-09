using MahApps.Metro.Controls;
using System.Windows;

namespace Uavcan.NET.Studio
{
    /// <summary>
    /// Interaction logic for ToolWindow.xaml
    /// </summary>
    public partial class ToolWindow : MetroWindow
    {
        public ToolWindow(UIElement element)
        {
            InitializeComponent();
            ContentArea.Content = element;
        }
    }
}
