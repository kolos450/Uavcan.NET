using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Uavcan.NET.Studio.Tools;
using MahApps.Metro.Controls;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using Uavcan.NET.Studio.Communication;
using System.ComponentModel;

namespace Uavcan.NET.Studio
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [Export]
    public sealed partial class MainWindow : MetroWindow, IDisposable, IPartImportsSatisfiedNotification
    {
        [Import]
        UavcanService _uavcan = null;

        [ImportMany]
        IEnumerable<IUavcanStudioToolProvider> _tools = null;

        [Import]
        CommunicationServicesProvider _communicationServicesProvider = null;

        INodeMonitor _nodeMonitor;
        HeartbeatService _heartbeatService;

        List<IDisposable> _disposables = new List<IDisposable>();

        public MainWindow()
        {
            InitializeComponent();

            IsBusy = false;
        }

        bool IsBusy
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
                if (uiElement is UserControl uc)
                {
                    toolWindow.Height = uc.Height;
                    toolWindow.Width = uc.Width;
                    uc.Height = double.NaN;
                    uc.Width = double.NaN;
                }
                toolWindow.Title = tool.ToolTitle;
                toolWindow.Show();

                if (toolWindow is IDisposable disposable)
                {
                    _disposables.Add(disposable);
                }
            }
        }

        public void RunTool(string toolTitle)
        {
            var tool = _tools.Where(x => x.ToolTitle == toolTitle)
                .Single();
            RunTool(tool);
        }

        public bool Active
        {
            get => !IsBusy;
            set => IsBusy = !value;
        }

        public void SyncState()
        {
            nudNodeId.Value = _uavcan.Engine.NodeID;
        }

        void InitializeWndTools()
        {
            var activeNodeHandles = _nodeMonitor.GetActiveNodes(
                TimeSpan.FromMilliseconds(DataTypes.Protocol.NodeStatus.OfflineTimeoutMs));

            var d = activeNodeHandles
                .ToObservableChangeSet()
                .Transform(_nodeMonitor.GetNodeDescriptor)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out var activeNodesObservable)
                .Subscribe();
            _disposables.Add(d);

            dgNodes.ItemsSource = activeNodesObservable;
        }

        void MenuItem_File_Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        void MenuItem_Help_About_Click(object sender, RoutedEventArgs e)
        {
            using (var aboutBox = new AboutBox())
            {
                aboutBox.ShowDialog(new WpfWindowWrapper(this));
            }
        }

        void ApplyNodeIdButton_Click(object sender, RoutedEventArgs e)
        {
            var nodeId = (byte)nudNodeId.Value;
            _uavcan.Engine.NodeID = nodeId;

            if (nodeId is 0)
                _heartbeatService.Stop();
            else
                _heartbeatService.Start();
        }

        public void Dispose()
        {
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
            _nodeMonitor = _communicationServicesProvider.Monitor;
            _heartbeatService = _communicationServicesProvider.HeartbeatService;

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
