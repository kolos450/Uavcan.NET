using Uavcan.NET.Dsdl.DataTypes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Uavcan.NET.Dsdl
{
    public class FileSystemUavcanTypeResolver : UavcanTypeResolverBase
    {
        public const string DsdlExtension = "uavcan";

        static readonly Regex _fileNameRegex = new Regex(
                @"^((?<DTID>[0-9]+)\.)?((?<NAME>[^\.]+)\.)((?<VER>[0-9]+\.[0-9]+)\.)?" + DsdlExtension + "$",
                RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        static readonly Regex _namespaceRegex = new Regex(
                @"^([a-z][a-z0-9_]*\.)*[a-z][a-z0-9_]*$",
                RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        readonly string _root;
        readonly string _rootNamespace;

        public FileSystemUavcanTypeResolver(string root)
        {
            _root = Path.GetFullPath(root);
            if (!_root.EndsWith("/") && !_root.EndsWith("/"))
            {
                _rootNamespace = Path.GetFileName(_root);
                _root += Path.DirectorySeparatorChar;
            }
            else
            {
                _rootNamespace = Path.GetFileName(Path.GetDirectoryName(_root));
            }

            ExploreTypes();
        }

        public UavcanTypeMeta ParseMeta(string path)
        {
            var name = Path.GetFileName(path);
            var match = _fileNameRegex.Match(name);
            if (!match.Success)
                throw new Exception($"Invalid file name [{path}]; expected pattern: [<default-dtid>.]<short-type-name>.[<major-version>.<minor-version>.]" + DsdlExtension);
            var meta = new UavcanTypeMeta();
            if (match.Groups["DTID"].Success)
                meta.DefaultDTID = int.Parse(match.Groups["DTID"].Value);
            if (match.Groups["VER"].Success)
                meta.Version = Version.Parse(match.Groups["VER"].Value);
            meta.Namespace = GetNamespace(path);
            meta.Name = match.Groups["NAME"].Value;
            return meta;
        }

        string GetNamespace(string path)
        {
            if (!path.StartsWith(_root))
                throw new Exception($"'{_root}' should contain '{path}'.");

            var ns = Path.GetDirectoryName(path);
            if (ns.Length <= _root.Length)
                return _rootNamespace;

            ns = ns.Substring(_root.Length);
            ns = ns.Replace(Path.DirectorySeparatorChar, '.').Replace(Path.AltDirectorySeparatorChar, '.');
            ns = _rootNamespace + "." + ns;
            ValidateNamespaceName(ns);
            return ns;
        }
        static void ValidateNamespaceName(string value)
        {
            if (!_namespaceRegex.IsMatch(value))
                throw new Exception($"Invalid namespace: '{value}'.");
        }

        string TryLocateNamespaceDirectory(string ns)
        {
            var namespaceComponents = ns.Split(new[] { '.' }, 2);
            string subPath = string.Empty;
            string rootNamespace;
            if (namespaceComponents.Length == 1)
            {
                rootNamespace = namespaceComponents[0];
            }
            else
            {
                rootNamespace = namespaceComponents[0];
                subPath = namespaceComponents[1].Replace('.', Path.DirectorySeparatorChar);
            }

            if (_rootNamespace == rootNamespace)
            {
                return Path.Combine(_root, subPath);
            }

            return null;
        }

        string TryLocateCompoundTypeDefiniton(string currentNs, string requiredTypeName)
        {
            var fullTypeName = requiredTypeName.IndexOf('.') == -1 ?
                currentNs + "." + requiredTypeName :
                requiredTypeName;

            var ns = fullTypeName.Substring(0, fullTypeName.LastIndexOf('.'));
            var nsDirectory = TryLocateNamespaceDirectory(ns);
            if (nsDirectory == null)
                return null;

            foreach (var file in Directory.EnumerateFiles(nsDirectory, "*." + DsdlExtension))
            {
                try
                {
                    var meta = ParseMeta(file);
                    if (string.Equals(meta.FullName, fullTypeName, StringComparison.Ordinal))
                        return file;
                }
                catch { }
            }

            return null;
        }

        protected override IUavcanType TryResolveTypeCore(string ns, string typeName)
        {
            var definitionPath = TryLocateCompoundTypeDefiniton(ns, typeName);
            if (definitionPath == null)
                return null;

            var meta = ParseMeta(definitionPath);

            Stream stream = null;
            try
            {
                stream = File.Open(definitionPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using (var reader = new StreamReader(stream))
                {
                    stream = null;
                    return DsdlParser.Parse(reader, meta);
                }
            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        Dictionary<int, List<UavcanTypeMeta>> _idToMetaLookup;

        void ExploreTypes()
        {
            _idToMetaLookup = new Dictionary<int, List<UavcanTypeMeta>>();

            var definitions = Directory.EnumerateFiles(_root, "*." + DsdlExtension, SearchOption.AllDirectories);
            foreach (var path in definitions)
            {
                try
                {
                    var meta = ParseMeta(path);
                    if (meta.DefaultDTID != null)
                    {
                        if (!_idToMetaLookup.TryGetValue(meta.DefaultDTID.Value, out var list))
                        {
                            list = new List<UavcanTypeMeta>();
                            _idToMetaLookup[meta.DefaultDTID.Value] = list;
                        }
                        list.Add(meta);
                    }
                }
                catch
                {
                    continue;
                }
            }
        }

        protected override IEnumerable<IUavcanTypeFullName> ResolveTypeNames(int dataTypeId)
        {
            if (!_idToMetaLookup.TryGetValue(dataTypeId, out var metas))
                return Enumerable.Empty<IUavcanTypeFullName>();
            return metas;
        }
    }
}
