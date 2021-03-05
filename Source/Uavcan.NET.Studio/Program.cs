using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Uavcan.NET.IO.Can.Drivers.Slcan;

namespace Uavcan.NET.Studio
{
    sealed class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            new Program().Run();
        }

        [Import]
        ShellService _shellService = null;

        void Run()
        {
            using (CreateContainer())
            {
                _shellService.RunApplication();
            }
        }

        CompositionContainer CreateContainer()
        {
            var assembly = typeof(Program).Assembly;
            var rootPath = Path.GetDirectoryName(new Uri(assembly.EscapedCodeBase).LocalPath);

            var catalogs = _EnumerateCatalogAssemblies(rootPath)
                .Select(x => new AssemblyCatalog(x))
                .Concat(new[] { new AssemblyCatalog(typeof(UsbTin).Assembly) })
                .Concat(new[] { new AssemblyCatalog(typeof(Program).Assembly) });

            var container = new CompositionContainer(new AggregateCatalog(catalogs), true);

            container.ComposeExportedValue<ExportProvider>(container);
            container.ComposeExportedValue<CompositionContainer>(container);
            container.ComposeExportedValue<ICompositionService>(container);
            container.SatisfyImportsOnce(this);

            return container;
        }

        static IEnumerable<string> _EnumerateCatalogAssemblies(string rootPath)
        {
            string pluginsPath = Path.Combine(rootPath, "Plugins");
            const string pluginEntryPattern = "Uavcan.NET.*.dll";
            if (Directory.Exists(pluginsPath))
            {
                foreach (var path in Directory.EnumerateFiles(pluginsPath, pluginEntryPattern, SearchOption.TopDirectoryOnly))
                {
                    yield return path;
                }
            }
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.IsTerminating)
            {
                const string FileName = "CRASH_LOG.txt";

                var filePath = Path.GetFullPath(FileName);
                try
                {
                    using (var tw = File.CreateText(filePath))
                    {
                        tw.WriteLine("***** Crash Dump *****");
                        tw.WriteLine();
                        tw.WriteLine(e.ExceptionObject?.ToString() ?? "NULL");
                    }

                    foreach (Form i in Application.OpenForms)
                        i.Close();
                }
                catch
                {
                }

                MessageBox.Show(
                    $"Unrecoverable error encountered.\n\n{FileName} file was created.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                Environment.Exit(1);
            }
        }
    }
}
