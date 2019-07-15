using MahApps.Metro.Controls;
using System;
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

            if (element is IDisposable disposable)
            {
                this.Closed += (o, e) =>
                {
                    disposable.Dispose();
                };
            }
        }
    }
}
