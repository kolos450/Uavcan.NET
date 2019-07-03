using CanardApp.Tools;
using CanardSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CanardApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [Export]
    public partial class MainWindow : Window
    {
        [ImportingConstructor]
        public MainWindow(
            [ImportMany] IEnumerable<ICanardToolProvider> tools)
        {
            InitializeComponent();

            foreach (var tool in tools)
            {
                var menuItem = new MenuItem
                {
                    Header = tool.ToolTitle,
                };

                miTools.Items.Add(menuItem);

                menuItem.Click += (o, e) => RunTool(tool);
            }
        }

        void RunTool(ICanardToolProvider tool)
        {
            var uiElement = tool.GetUIElement();
            if (uiElement != null)
            {
                var toolWindow = new ToolWindow(uiElement);
                toolWindow.Title = tool.ToolTitle;
                toolWindow.Show();
            }
        }

        public void Initialize(CanardInstance canardInstance)
        {
            busyIndicator.IsBusy = false;
        }

        void MenuItem_File_Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        void MenuItem_Help_About_Click(object sender, RoutedEventArgs e)
        {
            var aboutBox = new AboutBox();
            aboutBox.ShowDialog(new WpfWindowWrapper(this));
        }
    }
}
