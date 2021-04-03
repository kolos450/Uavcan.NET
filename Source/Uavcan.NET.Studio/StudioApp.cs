using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Uavcan.NET.IO.Can.Drivers;

namespace Uavcan.NET.Studio
{
    sealed class StudioApp : Application, IDisposable, IPartImportsSatisfiedNotification
    {
        [Import]
        MainWindow _mainWindow = null;

        [Import]
        UavcanService _uavcan = null;

        [Import]
        ICompositionService _compositionService = null;

        [Import]
        ExportProvider _exportProvider = null;

        ApplicationOptions _options;

        public StudioApp()
        {
            SetupThemes();

            ShutdownMode = ShutdownMode.OnMainWindowClose;
            Startup += StudioApp_Startup;
        }

        void StudioApp_Startup(object sender, StartupEventArgs e)
        {
            var driver = GetCanDriver();
            if (driver == null)
            {
                Shutdown();
                return;
            }
            _uavcan.AddDriver(driver);

            if (_options.NodeId is not null)
            {
                _uavcan.Engine.NodeID = _options.NodeId.Value;
                _mainWindow.SyncState();
            }

            MainWindow.Show();
            _mainWindow.Active = true;

            if (_options.ToolName is not null)
            {
                _mainWindow.RunTool(_options.ToolName);
            }
        }

        public void Run(ApplicationOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            Run();
        }

        void SetupThemes()
        {
            var resDictionary = new ResourceDictionary();
            resDictionary.Source = new Uri("Style.xaml", UriKind.Relative);
            Resources.MergedDictionaries.Add(resDictionary);
        }

        ICanInterface GetCanDriver()
        {
            var connectionString = _options.ConnectionString;
            if (connectionString is not null)
            {
                var (name, bitrate) = ParseConnectionString(connectionString);
                if (!string.IsNullOrEmpty(name))
                {
                    return CreateDriver(
                        name,
                        bitrate ?? Constants.DefaultCanBitrate);
                }
            }

            var wnd = new ConnectionWindow(_compositionService);
            if (wnd.ShowDialog() != true)
                return null;

            return wnd.ViewModel.Driver;
        }

        private ICanInterface CreateDriver(string name, int bitrate)
        {
            var portProviders = _exportProvider.GetExportedValues<ICanPortProvider>();
            var ports = portProviders
                .SelectMany(p => p.GetDriverPorts())
                .Where(x => x.DisplayName == name)
                .ToList();

            return ports.Count switch
            {
                0 => throw new ArgumentException($"Cannot find interface '{name}'.", nameof(name)),
                1 => OpenPort(ports[0], bitrate),
                _ => throw new ArgumentException($"Ambiguous interface '{name}'.", nameof(name)),
            };
        }

        private ICanInterface OpenPort(ICanPort canPort, int bitrate)
        {
            ICanInterface result = null;
            try
            {
                Task.Run(async () =>
                {
                    using (var cts = new CancellationTokenSource(Constants.InterfaceConnectionTimeoutMs))
                    {
                        result = await canPort.OpenAsync(bitrate, cts.Token).ConfigureAwait(false);
                    }
                }).Wait();
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Cannot open port {canPort.DisplayName} with bitrate {bitrate}.", ex);
            }

            return result;
        }

        private static (string name, int? bitrate) ParseConnectionString(string input)
        {
            var parts = input.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim());

            string name = null;
            int? bitrate = null;

            int partIndex = 0;
            foreach (var part in parts)
            {
                if (partIndex++ == 0)
                {
                    name = part;
                }
                else
                {
                    var elements = part.Split('=', 2);
                    if (elements.Length != 2)
                        continue;

                    switch (elements[0].ToLower())
                    {
                        case "bitrate":
                            try
                            {
                                bitrate = int.Parse(elements[1], NumberStyles.None);
                            }
                            catch
                            {
                                throw new ArgumentException($"Cannot parse bitrate '{elements[1]}'.");
                            }
                            break;
                    }
                }
            }

            return (name, bitrate);
        }

        public void Dispose()
        {
        }

        public void OnImportsSatisfied()
        {
            MainWindow = _mainWindow;
        }
    }
}
