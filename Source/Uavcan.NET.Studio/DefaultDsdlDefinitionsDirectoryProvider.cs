using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;

namespace Uavcan.NET.Studio
{
    [Export(typeof(IDsdlDefinitionsDirectoryProvider))]
    sealed class DefaultDsdlDefinitionsDirectoryProvider : IDsdlDefinitionsDirectoryProvider
    {
        public IEnumerable<string> Directories
        {
            get
            {
                var assembly = typeof(Program).Assembly;
                var rootPath = Path.GetDirectoryName(new Uri(assembly.EscapedCodeBase).LocalPath);
                while (rootPath != null)
                {
                    var dsdlDefinitionsPath = Path.Combine(rootPath, "DsdlDefinitions");
                    if (Directory.Exists(dsdlDefinitionsPath))
                    {
                        foreach (var dir in Directory.EnumerateDirectories(dsdlDefinitionsPath))
                            yield return dir;
                    }

                    rootPath = Path.GetDirectoryName(rootPath);
                }
            }
        }
    }
}
