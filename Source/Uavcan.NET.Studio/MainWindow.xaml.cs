using Uavcan.NET.Studio.Tools;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;

namespace Uavcan.NET.Studio
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [Export]
    public partial class MainWindow : MetroWindow, IDisposable, IPartImportsSatisfiedNotification
    {
        [Import]
        UavcanService _uavcan = null;

        [ImportMany]
        IEnumerable<IUavcanStudioToolProvider> _tools = null;

        OnlineNodesTool _onlineNodesTool;

        List<IDisposable> _disposables = new List<IDisposable>();

        public MainWindow()
        {
            InitializeComponent();

            _IsBusy = false;
        }

        bool _IsBusy
        {
            get => bBusyIndicator.Visibility == Visibility.Visible;
            set => bBusyIndicator.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }

        void RunTool(IUavcanStudioToolProvider tool)
        {
            var uiElement = tool.CreateUIElement(_uavcan.Engine);
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

        public bool Active
        {
            get => !_IsBusy;
            set => _IsBusy = !value;
        }

        void InitializeWndTools()
        {
            _onlineNodesTool = new OnlineNodesTool(_uavcan.Engine);
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
            _uavcan.Engine.NodeID = nodeId;
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

        public void OnImportsSatisfied()
        {
            foreach (var tool in _tools)
            {
                var menuItem = new MenuItem
                {
                    Header = tool.ToolTitle,
                };

                miTools.Items.Add(menuItem);

                menuItem.Click += (o, e) => RunTool(tool);
            }

            InitializeWndTools();
        }
    }
}
