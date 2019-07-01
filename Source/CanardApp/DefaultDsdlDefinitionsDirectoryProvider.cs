using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardApp
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
                        yield return dsdlDefinitionsPath;
                        yield break;
                    }

                    rootPath = Path.GetDirectoryName(rootPath);
                }
            }
        }
    }
}
