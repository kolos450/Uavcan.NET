using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Text;
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
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;

            try
            {
                new Program().Run();
            }
            catch (Exception ex)
            {
                LogUnhandledException(ex);
            }
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

        static StreamWriter _crashLogWriter;
        static int _crashLogCounter = 0;

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.IsTerminating)
            {
                LogUnhandledException(e.ExceptionObject);
                Environment.Exit(1);
            }
        }

        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            LogUnhandledException(e.Exception);
            Environment.Exit(1);
        }

        private static void LogUnhandledException(object exceptionObject)
        {
            const string FileName = "CRASH_LOG.txt";

            lock (typeof(Program))
            {
                bool disposeWriter = false;
                try
                {
                    if (_crashLogWriter is null)
                    {
                        disposeWriter = true;
                        var filePath = Path.GetFullPath(FileName);

                        if (_crashLogCounter++ == 0)
                        {
                            _crashLogWriter = File.CreateText(filePath);
                            _crashLogWriter.WriteLine("***** Crash Dump *****");
                            _crashLogWriter.WriteLine();
                        }
                        else
                        {
                            _crashLogWriter = File.AppendText(filePath);
                            _crashLogWriter.WriteLine();
                            _crashLogWriter.WriteLine("**********************");
                            _crashLogWriter.WriteLine("**********************");
                            _crashLogWriter.WriteLine("**********************");
                            _crashLogWriter.WriteLine();
                        }
                    }

                    _crashLogWriter.WriteLine(CreateExceptioMessage(exceptionObject) ?? "NULL");

                    foreach (Form i in Application.OpenForms)
                        i.Close();
                }
                catch
                {
                }
                finally
                {
                    if (disposeWriter)
                    {
                        _crashLogWriter?.Dispose();
                        _crashLogWriter = null;
                    }
                }
            }

            MessageBox.Show(
                $"Unrecoverable error encountered.\n\n{FileName} file was created.",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private static string CreateExceptioMessage(object exceptionObject)
        {
            if (exceptionObject is null)
                return null;

            try
            {
                return exceptionObject.ToString();
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append("Cannot get ");
                sb.Append(exceptionObject.GetType().FullName);
                sb.AppendLine(" message:");
                sb.AppendLine(ex.ToString());

                try
                {
                    bool subsectionTitleWritten = false;
                    foreach (var nested in EnumerateNestedExceptions(exceptionObject))
                    {
                        if (!subsectionTitleWritten)
                        {
                            sb.AppendLine();
                            sb.AppendLine("Nested exceptions:");
                            subsectionTitleWritten = true;
                        }

                        sb.AppendLine(nested.ToString());
                    }
                }
                catch { }

                return sb.ToString();
            }
        }

        private static IEnumerable<Exception> EnumerateNestedExceptions(object exceptionObject)
        {
            switch (exceptionObject)
            {
                case CompositionException compositionException:
                    return compositionException.RootCauses;
                case AggregateException aggregateException:
                    return aggregateException.InnerExceptions;
            }

            return Enumerable.Empty<Exception>();
        }
    }
}
