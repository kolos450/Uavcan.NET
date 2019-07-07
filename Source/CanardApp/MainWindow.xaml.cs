using CanardApp.DataTypes.Protocol;
using CanardApp.Presentation;
using CanardApp.Tools;
using CanardSharp;
using MahApps.Metro.Controls;
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
    public partial class MainWindow : MetroWindow
    {
        [ImportingConstructor]
        public MainWindow(
            [ImportMany] IEnumerable<ICanardToolProvider> tools)
        {
            InitializeComponent();

            IsBusy = false;

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

        public bool IsBusy
        {
            get => bBusyIndicator.Visibility == Visibility.Visible;
            set => bBusyIndicator.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
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
            dgNodes.ItemsSource = new OnlineNodeModel[]
            {
                new OnlineNodeModel
                {
                    NodeId = 3,
                    Name = "dsfsd",
                    Mode = NodeStatus.ModeKind.Operational,
                    Health = NodeStatus.HealthKind.Ok,
                    Uptime = TimeSpan.FromMilliseconds(32422),
                    VSSC = 34,
                }
                ,new OnlineNodeModel
                {
                    NodeId = 3,
                    Name = "dsfsd",
                    Mode = NodeStatus.ModeKind.Operational,
                    Health = NodeStatus.HealthKind.Ok,
                    Uptime = TimeSpan.FromMilliseconds(32422),
                    VSSC = 34,
                }
                ,new OnlineNodeModel
                {
                    NodeId = 3,
                    Name = "dsfsd",
                    Mode = NodeStatus.ModeKind.Operational,
                    Health = NodeStatus.HealthKind.Ok,
                    Uptime = TimeSpan.FromMilliseconds(32422),
                    VSSC = 34,
                },
            };

            dgDebug.ItemsSource = new DebugMessageModel[]
            {
                new DebugMessageModel
                {
                    NodeId=1,
                    Level = DataTypes.Protocol.Debug.LogLevel.ValueKind.Info,
                    Source = "dsf",
                    Text = "dfsdf",
                    Time = DateTime.Now,
                }
            };

            IsBusy = false;
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
