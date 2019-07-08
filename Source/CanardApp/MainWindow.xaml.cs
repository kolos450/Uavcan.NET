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
    public partial class MainWindow : MetroWindow, IDisposable
    {
        CanardInstance _canardInstance;

        OnlineNodesTool _onlineNodesTool;

        List<IDisposable> _disposables = new List<IDisposable>();

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
            var uiElement = tool.CreateUIElement(_canardInstance);
            if (uiElement != null)
            {
                var toolWindow = new ToolWindow(uiElement);
                toolWindow.Title = tool.ToolTitle;
                toolWindow.Show();

                if (toolWindow is IDisposable disposable)
                {
                    _disposables.Add(disposable);
                }
            }
        }

        public void Initialize(CanardInstance canardInstance)
        {
            _canardInstance = canardInstance;

            InitializeWndTools();

            IsBusy = false;
        }

        void InitializeWndTools()
        {
            _onlineNodesTool = new OnlineNodesTool(_canardInstance);
            dgNodes.ItemsSource = _onlineNodesTool.OnlineNodes;
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

        void ApplyNodeIdButton_Click(object sender, RoutedEventArgs e)
        {
            var nodeId = (byte)nudNodeId.Value;
            _canardInstance.NodeID = nodeId;
        }

        public void Dispose()
        {
            if (_onlineNodesTool != null)
            {
                _onlineNodesTool.Dispose();
                _onlineNodesTool = null;
            }

            if (_disposables != null)
            {
                foreach (var disposable in _disposables)
                {
                    disposable.Dispose();
                }
                _disposables = null;
            }
        }
    }
}
