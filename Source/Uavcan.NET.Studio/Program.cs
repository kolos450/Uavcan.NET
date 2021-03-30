using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using Uavcan.NET.IO.Can.Drivers.Slcan;
using Uavcan.NET.Studio.CommandLine;
using Uavcan.NET.Studio.Framework;

namespace Uavcan.NET.Studio
{
    sealed class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                return new Program().Run(args);
            }
            catch (Exception ex)
            {
                LogUnhandledException(ex);
                return 1;
            }
        }

        [Import]
        ShellService _shellService = null;

        int Run(string[] args)
        {
            var options = ApplicationOptions.Default;

            if (args.Length != 0)
            {
                int exitCode = ProcessCommandLineArgs(args, out options);

                if (exitCode is not 0)
                {
                    return exitCode;
                }
            }

            using (CreateContainer())
            {
                _shellService.RunApplication(options);
            }

            return 0;
        }

        private static int ProcessCommandLineArgs(string[] args, out ApplicationOptions options)
        {
            var connectionStringOption = new Option<string>(
                "--connection-string",
                description: "Connection string.");
            var toolOption = new Option<string>(
                "--tool",
                "Open a specific custom tool.");

            var rootCommand = new RootCommand
            {
                connectionStringOption,
                toolOption
            };

            rootCommand.Description = Constants.ProductName;

            var commandHandler = new ArgumentsCommandHandler();
            rootCommand.Handler = commandHandler;

            var console = new ArgumentsConsole();
            var exitCode = rootCommand.Invoke(args, console);

            if (console.Out.Builder.Length != 0)
            {
                FlexibleMessageBox.FONT = FontUtilities.GetFont(new[] { "Consolas", "Courier New" }, 9);
                FlexibleMessageBox.Show(console.Out.ToString(), Constants.ProductName, MessageBoxButtons.OK);
            }

            var parseResult = commandHandler.ParseResult;
            options = new ApplicationOptions(
                parseResult.FindResultFor(connectionStringOption)?.GetValueOrDefault<string>(),
                parseResult.FindResultFor(toolOption)?.GetValueOrDefault<string>());

            return exitCode;
        }

        CompositionContainer CreateContainer()
        {
            var assembly = typeof(Program).Assembly;
            var rootPath = Path.GetDirectoryName(new Uri(assembly.EscapedCodeBase).LocalPath);

            var catalogs = _EnumerateCatalogAssemblies(rootPath)
                .Select(x => new AssemblyCatalog(Assembly.LoadFrom(x)))
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
