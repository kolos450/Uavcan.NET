using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace CanardSharp.Testing.Framework
{
    public static class ApplicationLauncher
    {
        public static int Run(
            string fileName,
            string arguments,
            string workingDirectory,
            TextWriter outputTextWriter)
        {
            using (var process = new Process())
            {
                var psi = process.StartInfo;

                psi.FileName = fileName;
                psi.Arguments = arguments;
                psi.WorkingDirectory = workingDirectory;
                psi.UseShellExecute = false;
                psi.RedirectStandardError = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.StandardOutputEncoding = Encoding.UTF8;

                process.OutputDataReceived +=
                    (object sender, DataReceivedEventArgs e) =>
                    {
                        string data = e.Data;
                        if (string.IsNullOrEmpty(data))
                            return;

                        outputTextWriter.WriteLine(data);
                    };

                try
                {
                    process.Start();
                }
                catch (Exception e)
                {
                    throw new Exception(string.Format(
                        "Unable to execute application \"{0}\".",
                        fileName),
                        e);
                }

                process.BeginOutputReadLine();

                const int PollInterval = 100; // ms

                for (int i = 0; ; ++i)
                {
                    if (process.WaitForExit(PollInterval))
                        break;
                }

                return process.ExitCode;
            }
        }
    }
}
