using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CanardApp
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
            LoadPackages();

            _shellService.RunApplication();
        }

        void LoadPackages()
        {
            var assembly = typeof(Program).Assembly;
            var rootPath = Path.GetDirectoryName(new Uri(assembly.EscapedCodeBase).LocalPath);

            var currentAssemblyCatalog = new AssemblyCatalog(typeof(Program).Assembly);

            var catalogs = _EnumerateCatalogAssemblies(rootPath)
                .Select(x => new AssemblyCatalog(x))
                .Concat(new[] { currentAssemblyCatalog });

            var container = new CompositionContainer(new AggregateCatalog(catalogs), true);

            container.ComposeExportedValue<ExportProvider>(container);
            container.ComposeExportedValue<CompositionContainer>(container);
            container.ComposeExportedValue<ICompositionService>(container);
            container.SatisfyImportsOnce(this);
        }

        static IEnumerable<string> _EnumerateCatalogAssemblies(string rootPath)
        {
            string filePattern = "CanardApp.*.dll";

            var query = Directory.EnumerateFiles(rootPath, filePattern);

            string packagesPath = Path.Combine(rootPath, "Plugins");
            if (Directory.Exists(packagesPath))
                query = query.Concat(Directory.EnumerateFiles(packagesPath, filePattern));

            query = query.Where(path => !path.EndsWith(".Contracts.dll", StringComparison.OrdinalIgnoreCase));

            return query;
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
