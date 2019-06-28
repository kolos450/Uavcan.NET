﻿using CanardSharp.Dsdl;
using CanardSharp.Dsdl.DataTypes;
using CanardSharp.Testing.Framework;
using Irony.Parsing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SerializationTestGenerator
{
    class Program
    {
        Stack<string> _namespacesStack = new Stack<string>();
        List<TestType> _types = new List<TestType>();
        Dictionary<CompositeDsdlTypeBase, TestType> _compoundTypesLookup = new Dictionary<CompositeDsdlTypeBase, TestType>();

        public string SourceText { get; set; }
        public string PyUavcanDirectory { get; set; }

        static void Main(string[] args)
        {
            const string pyuavcanPath = @"C:\Sources\pyuavcan";
            var pyVersion = GetPythonVersion(pyuavcanPath);
            if (pyVersion == null)
                throw new InvalidOperationException("Cannot execute Python.");
            Console.WriteLine($"Python {pyVersion}");

            var dsdlRoot = Path.Combine(pyuavcanPath, @"uavcan\dsdl_files");
            var dsdlRootCopy = IOUtils.GetTemporaryDirectory();
            IOUtils.DirectoryCopy(dsdlRoot, dsdlRootCopy, true);

            try
            {
                var root = Assembly.GetExecutingAssembly().Location;
                while (Path.GetFileName(root) != "SerializationTestGenerator")
                    root = Path.GetDirectoryName(root);
                var testsTextPath = Path.Combine(root, "Tests.txt");
                var testsText = File.ReadAllText(testsTextPath);

                var grammar = new TestSchemeFileGrammar();
                var parser = new Parser(grammar);
                var tree = parser.Parse(testsText);

                if (tree.Status != ParseTreeStatus.Parsed)
                {
                    Console.WriteLine($"Cannot parse source file {testsTextPath}.");

                    foreach (var msg in tree.ParserMessages)
                    {
                        Console.WriteLine($"{msg} at {msg.Location}.");
                    }
                }

                Directory.CreateDirectory(dsdlRoot);
                var schemePath = Path.Combine(root, @"..\CanardSharp.Dsdl.Testing\Serialization\AutogeneratedTests\Scheme.txt");
                var typesPath = Path.Combine(root, @"..\CanardSharp.Dsdl.Testing\Serialization\AutogeneratedTests\Types.cs");
                var testsCsPath = Path.Combine(root, @"..\CanardSharp.Dsdl.Testing\Serialization\AutogeneratedTests\Tests.cs");

                var prog = new Program
                {
                    SourceText = testsText,
                    PyUavcanDirectory = pyuavcanPath,
                };

                prog.CollectTestTypes(tree.Root);
                prog.CompileDsdlTypes();
                prog.BuildCompoundTypesLookup();
                prog.CreateCSharpNames();

                prog.BuildScheme(schemePath);
                prog.BuildDsdlDefinitions(dsdlRoot);
                prog.BuildTypes(typesPath);

                prog.BuildTestMethods(testsCsPath);
            }
            finally
            {
                IOUtils.DirectoryDelete(dsdlRoot);
                IOUtils.DirectoryCopy(dsdlRootCopy, dsdlRoot, true);
                IOUtils.DirectoryDelete(dsdlRootCopy);
            }
        }

        static Version GetPythonVersion(string workingDirectory = null)
        {
            var versionString = GetPythonOutput("-V", workingDirectory);
            if (versionString == null)
                return null;
            if (!versionString.StartsWith("Python ", StringComparison.Ordinal))
                return null;
            if (!Version.TryParse(versionString.Substring("Python ".Length), out var ver))
                return null;
            return ver;
        }

        static string GetPythonOutput(string arguments, string workingDirectory = null)
        {
            using (var writer = new StringWriter())
            {
                if (ApplicationLauncher.Run("python", arguments, workingDirectory, writer) != 0)
                    return null;
                return writer.ToString();
            }
        }

        void BuildTestMethods(string path)
        {
            var content = string.Join(Environment.NewLine, BuildTestMethods());
            File.WriteAllText(path, content);
        }

        IEnumerable<string> BuildTestMethods()
        {
            yield return "using Microsoft.VisualStudio.TestTools.UnitTesting;";
            yield return "";
            yield return "namespace CanardSharp.Dsdl.Testing.Serialization.AutogeneratedTests";
            yield return "{";
            yield return "    [TestClass]";
            yield return "    public sealed class Tests";
            yield return "    {";
            int counter = 0;
            foreach (var t in _types)
            {
                foreach (var tcase in t.TestCases)
                {
                    yield return "        [TestMethod]";
                    yield return $"        public void {t.Name}_{counter++}()";
                    yield return "        {";

                    foreach (var i in BuildTestMethod(t, tcase))
                    {
                        yield return "            " + i;
                    }

                    yield return "        }";
                    yield return "";
                }
            }
            yield return "    }";
            yield return "}";
        }

        IEnumerable<string> BuildTestMethod(TestType t, TestCase tcase)
        {
            string pySerialized = PySerialize(t, tcase).Trim();

            if (!(t.Body is MessageType scheme))
                throw new NotSupportedException("Message is the only supported type.");

            yield return $"var obj = new {t.CSharpName} {{ " +
                string.Join(", ", BuildTestMethodMembers(scheme.Fields, tcase.Tree.FindChild("members"))) +
                " };";

            var doRoundtripTest = tcase.Directives.Contains("disableRoundtripTest") ? "false" : "true";

            yield return $"SerializationTestEngine.Test(obj, \"{pySerialized}\", doRoundtripTest: {doRoundtripTest});";
        }

        IEnumerable<string> BuildTestMethodMembers(IReadOnlyList<DsdlField> fields, ParseTreeNode parseTreeNode)
        {
            if (parseTreeNode == null)
                yield break;

            var nonVoidFields = fields.Where(x => !(x.Type is VoidDsdlType)).ToList();
            if (nonVoidFields.Count != parseTreeNode.ChildNodes.Count)
                throw new InvalidOperationException($"Invalid members count at position {parseTreeNode.Span.Location}");

            for (int i = 0; i < nonVoidFields.Count; i++)
            {
                var field = nonVoidFields[i];
                var node = parseTreeNode.ChildNodes[i];

                if (!IsNullNode(node))
                    yield return $"{field.Name} = " + BuildTestMethodMemberElement(field.Type, node);
            }
        }

        static bool IsNullNode(ParseTreeNode node)
        {
            if (node.Term.Name != "member")
                return false;
            if (node.ChildNodes.Count != 1)
                return false;
            if (node.ChildNodes[0].Token?.Text != "null")
                return false;

            return true;
        }

        string BuildTestMethodMemberElement(DsdlType type, ParseTreeNode node)
        {
            switch (type)
            {
                case PrimitiveDsdlType _:
                    return node.GetText(SourceText);

                case ArrayDsdlType adt:
                    var nestesNodes = node.FindChild("members");
                    if (nestesNodes == null)
                        return $"new {GetCSharpType(adt, false)} {{ }}";
                    var arrayContent = nestesNodes.ChildNodes.Select(x => BuildTestMethodMemberElement(adt.ElementType, x));
                    return $"new {GetCSharpType(adt, false)} {{ {string.Join(", ", arrayContent)} }}";

                case CompositeDsdlTypeBase cdt:
                    var t = _compoundTypesLookup[cdt];
                    return $"new {t.CSharpName} {{ " +
                        string.Join(", ", BuildTestMethodMembers(cdt.Fields, node.FindChild("members"))) +
                    " }";

                default:
                    throw new InvalidOperationException();
            }
        }

        string PySerialize(TestType t, TestCase tcase)
        {
            var content = BuildPythonSerializationScript(t, tcase);
            var file = Path.Combine(PyUavcanDirectory, Path.GetRandomFileName() + ".py");
            try
            {
                File.WriteAllText(file, content);
                var output = GetPythonOutput($"\"{file}\"", PyUavcanDirectory);
                if (output == null)
                    throw new Exception("Cannot get Python output.");
                return output;
            }
            finally
            {
                IOUtils.WithRetries(() => File.Delete(file));
            }
        }

        string BuildPythonSerializationScript(TestType t, TestCase tcase)
        {
            var initializer = BuildPythonInitializer(t, tcase.Tree);

            return $@"import uavcan

def bytes_from_bits(s):
    return bytearray(int(s[i:i + 8], 2) for i in range(0, len(s), 8))

msg = {initializer}

payload_bits = msg._pack()
if len(payload_bits) & 7:
    payload_bits += ""0"" * (8 - (len(payload_bits) & 7))
payload = bytes_from_bits(payload_bits)

print(''.join('{{:02x}}'.format(x) for x in payload))";
        }

        string BuildPythonInitializer(TestType t, ParseTreeNode tcase)
        {
            if (!(t.Body is MessageType scheme))
                throw new NotSupportedException("Message is the only supported type.");

            return $"{t.Namespace}.{t.Name} (" +
                string.Join(", ", BuildPythonInitializerMembers(scheme.Fields, tcase.FindChild("members"))) +
                ")";
        }

        IEnumerable<string> BuildPythonInitializerMembers(IReadOnlyList<DsdlField> fields, ParseTreeNode parseTreeNode)
        {
            if (parseTreeNode == null)
                yield break;

            var nonVoidFields = fields.Where(x => !(x.Type is VoidDsdlType)).ToList();
            if (nonVoidFields.Count != parseTreeNode.ChildNodes.Count)
                throw new InvalidOperationException($"Invalid members count at position {parseTreeNode.Span.Location}");

            for (int i = 0; i < nonVoidFields.Count; i++)
            {
                var field = nonVoidFields[i];
                var node = parseTreeNode.ChildNodes[i];

                if (!IsNullNode(node))
                    yield return $"{field.Name} = " + BuildPythonInitializerMemberElement(field.Type, node);
            }
        }

        string BuildPythonInitializerMemberElement(DsdlType type, ParseTreeNode node)
        {
            switch (type)
            {
                case PrimitiveDsdlType _:
                    var text = node.GetText(SourceText);
                    switch (text)
                    {
                        case "true":
                            text = "True";
                            break;
                        case "false":
                            text = "False";
                            break;
                        case "null":
                            text = "None";
                            break;
                    }

                    if (text.EndsWith("f", StringComparison.OrdinalIgnoreCase) &&
                        node.FindToken()?.Value is float) // Remove float 'f' suffix.
                        text = text.Substring(0, text.Length - 1);

                    return text;

                case ArrayDsdlType adt:
                    var nestesNodes = node.FindChild("members");
                    if (nestesNodes == null)
                        return "[]";
                    var arrayContent = nestesNodes.ChildNodes.Select(x => BuildPythonInitializerMemberElement(adt.ElementType, x));
                    return $"[{string.Join(", ", arrayContent)}]";

                case CompositeDsdlTypeBase cdt:
                    var t = _compoundTypesLookup[cdt];
                    return $"{t.Namespace}.{t.Name}(" +
                        string.Join(", ", BuildPythonInitializerMembers(cdt.Fields, node.FindChild("members"))) +
                    ")";

                default:
                    throw new InvalidOperationException();
            }
        }

        void BuildCompoundTypesLookup()
        {
            foreach (var t in _types)
            {
                switch (t.Body)
                {
                    case MessageType mt:
                        _compoundTypesLookup.Add(mt, t);
                        break;

                    case ServiceType st:
                        _compoundTypesLookup.Add(st.Request, t);
                        _compoundTypesLookup.Add(st.Response, t);
                        break;

                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        void CreateCSharpNames()
        {
            var dict = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var t in _types)
            {
                string csharpName;
                if (dict.TryGetValue(t.Name, out var counter))
                {
                    do
                    {
                        counter++;
                        csharpName = t.CSharpName = t.Name + "_" + counter;
                    }
                    while (!dict.ContainsKey(csharpName));
                }
                else
                {
                    csharpName = t.Name;
                }

                t.CSharpName = csharpName;
                dict[t.Name] = counter;
            }
        }

        void BuildTypes(string typesPath)
        {
            var builder = new StringBuilder();

            builder.AppendLine("using System.Runtime.Serialization;");
            builder.AppendLine();
            builder.AppendLine("namespace CanardSharp.Dsdl.Testing.Serialization.AutogeneratedTests");
            builder.AppendLine("{");

            foreach (var t in _types)
            {
                switch (t.Body)
                {
                    case MessageType mt:
                        builder.AppendLine($"    [DataContract(Name = \"{t.Name}\", Namespace = \"{t.Namespace}\")]");
                        builder.AppendLine($"    sealed class {t.CSharpName}");
                        builder.AppendLine("    {");
                        foreach (var i in BuildCSharpMembers(mt))
                            builder.AppendLine("        " + i);
                        builder.AppendLine("    }");
                        builder.AppendLine();
                        break;

                    case ServiceType st:
                        builder.AppendLine($"    [DataContract(Name = \"{t.Name}.Request\", Namespace = \"{t.Namespace}\")]");
                        builder.AppendLine($"    sealed class {t.CSharpName}_Request");
                        builder.AppendLine("    {");
                        foreach (var i in BuildCSharpMembers(st.Request))
                            builder.AppendLine("        " + i);
                        builder.AppendLine("    }");
                        builder.AppendLine();

                        builder.AppendLine($"    [DataContract(Name = \"{t.Name}.Response\", Namespace = \"{t.Namespace}\")]");
                        builder.AppendLine($"    sealed class {t.CSharpName}_Response");
                        builder.AppendLine("    {");
                        foreach (var i in BuildCSharpMembers(st.Request))
                            builder.AppendLine("        " + i);
                        builder.AppendLine("    }");
                        builder.AppendLine();
                        break;

                    default:
                        throw new InvalidOperationException();
                }
            }

            builder.AppendLine("}");

            File.WriteAllText(typesPath, builder.ToString());
        }

        IEnumerable<string> BuildCSharpMembers(CompositeDsdlTypeBase type)
        {
            foreach (var m in type.Fields)
            {
                var fieldType = GetCSharpType(m.Type, type.IsUnion);
                if (fieldType != null)
                {
                    yield return $"[DataMember(Name = \"{m.Name}\")]";
                    yield return $"public {fieldType} {m.Name} {{ get; set; }}";
                }
            }
        }

        string GetCSharpType(DsdlType type, bool nullable)
        {
            switch (type)
            {
                case VoidDsdlType _:
                    return null;

                case PrimitiveDsdlType t:
                    return nullable ? GetCSharpType(t) + "?" : GetCSharpType(t);

                case ArrayDsdlType t:
                    return GetCSharpType(t.ElementType, false) + "[]";

                case CompositeDsdlTypeBase t:
                    return _compoundTypesLookup[t].CSharpName;

                default:
                    throw new InvalidOperationException();
            }
        }

        string GetCSharpType(PrimitiveDsdlType type)
        {
            switch (type)
            {
                case BooleanDsdlType _:
                    return "bool";

                case IntDsdlType t:
                    if (t.MaxBitlen <= 8)
                        return "sbyte";
                    else if (t.MaxBitlen <= 16)
                        return "short";
                    else if (t.MaxBitlen <= 32)
                        return "int";
                    else if (t.MaxBitlen <= 64)
                        return "long";
                    else
                        throw new InvalidOperationException();

                case UIntDsdlType t:
                    if (t.MaxBitlen <= 8)
                        return "byte";
                    else if (t.MaxBitlen <= 16)
                        return "ushort";
                    else if (t.MaxBitlen <= 32)
                        return "uint";
                    else if (t.MaxBitlen <= 64)
                        return "ulong";
                    else
                        throw new InvalidOperationException();

                case FloatDsdlType t:
                    if (t.MaxBitlen == 16 || t.MaxBitlen == 32)
                        return "float";
                    else if (t.MaxBitlen == 64)
                        return "double";
                    else
                        throw new InvalidOperationException();

                default:
                    throw new InvalidOperationException();
            }
        }

        void BuildDsdlDefinitions(string dsdlPath)
        {
            var gns = _types.GroupBy(x => x.Namespace);
            foreach (var i in gns)
            {
                var dir = Path.Combine(dsdlPath, i.Key.Replace('.', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(dir);

                foreach (var t in i)
                {
                    var file = Path.Combine(dir, t.Name + ".uavcan");
                    var content = _dsdlSpacesNormalizer.Replace(t.BodyText, "$1");
                    content = content.Trim();
                    File.WriteAllText(file, content);
                }
            }
        }

        Regex _dsdlSpacesNormalizer = new Regex("(\r\n|\n)([ \t]*)",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        void BuildScheme(string schemePath)
        {
            var builder = new StringBuilder();

            var gns = _types.GroupBy(x => x.Namespace);
            foreach (var i in gns)
            {
                builder.Append("namespace ");
                builder.AppendLine(i.Key);
                builder.AppendLine("{");

                foreach (var t in i)
                {
                    builder.Append("    type ");
                    builder.AppendLine(t.Name);
                    builder.AppendLine("    {");

                    var content = _dsdlSpacesNormalizer.Replace(t.BodyText, "$1        ");
                    builder.Append("        ");
                    builder.Append(content.Trim());
                    builder.AppendLine();

                    builder.AppendLine("    }");
                }

                builder.AppendLine("}");
            }

            File.WriteAllText(schemePath, builder.ToString());
        }

        void CompileDsdlTypes()
        {
            var resolver = new TestTypeResolver(_types);
            resolver.ResolveAll();
        }

        class TestTypeResolver : UavcanTypeResolverBase
        {
            Dictionary<string, TestType> _lookup =
                new Dictionary<string, TestType>(StringComparer.Ordinal);

            public TestTypeResolver(IEnumerable<TestType> types)
            {
                foreach (var i in types)
                {
                    var fullName = i.Namespace + "." + i.Name;
                    _lookup.Add(fullName, i);
                }
            }

            protected override IUavcanType TryResolveTypeCore(string ns, string typeName)
            {
                TestType type;
                if (!_lookup.TryGetValue(typeName, out type) &&
                    !_lookup.TryGetValue(ns + "." + typeName, out type))
                    return null;

                return TryResolveType(type);
            }

            IUavcanType TryResolveType(TestType type)
            {
                if (type.Body == null)
                {
                    using (var reader = new StringReader(type.BodyText))
                    {
                        var meta = new UavcanTypeMeta
                        {
                            Name = type.Name,
                            Namespace = type.Namespace,
                        };
                        var body = DsdlParser.Parse(reader, meta);
                        type.Body = body;
                    }
                }

                return type.Body;
            }

            public void ResolveAll()
            {
                foreach (var i in _lookup.Values)
                {
                    TryResolveType(i.Namespace, i.Name);
                }
            }
        }

        void CollectTestTypes(ParseTreeNode node)
        {
            switch (node.Term.Name)
            {
                case "compilation_unit":
                    foreach (var i in node.ChildNodes)
                    {
                        ProcessNamespace(i);
                    }
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        void ProcessNamespace(ParseTreeNode node)
        {
            switch (node.Term.Name)
            {
                case "namespace_declarations_opt":
                    foreach (var i in node.ChildNodes)
                        ProcessNamespace(i);
                    break;
                case "namespace_declaration":
                    var namespaceName = node.FindChild("qualified_identifier").GetText(SourceText);

                    var nsMembers = node.FindChild("namespace_member_declarations");
                    if (nsMembers != null)
                    {
                        _namespacesStack.Push(namespaceName);

                        foreach (var i in nsMembers.ChildNodes)
                        {
                            switch (i.Term.Name)
                            {
                                case "namespace_declaration":
                                    ProcessNamespace(i);
                                    break;
                                case "type_declaration":
                                    ProcessType(i);
                                    break;
                            }
                        }

                        _namespacesStack.Pop();
                    }
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        void ProcessType(ParseTreeNode node)
        {
            switch (node.Term.Name)
            {
                case "type_declaration":
                    var typeName = node.FindChild("Identifier").FindTokenAndGetText();
                    var typeBody = node.FindChild("type_body_content").FindTokenAndGetText();
                    var typeTests = node.FindChild("type_test_cases_opt");
                    var testCases = CreateTestCases(typeTests);
                    CreateType(string.Join(".", _namespacesStack.Reverse()), typeName, typeBody, testCases);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }



        void CreateType(string ns, string name, string body, IEnumerable<TestCase> testCases)
        {
            var type = new TestType
            {
                Namespace = ns,
                Name = name,
                BodyText = body,
                TestCases = testCases,
            };

            _types.Add(type);
        }

        private static IEnumerable<TestCase> CreateTestCases(ParseTreeNode testCasesRoot)
        {
            if (testCasesRoot == null)
                return Enumerable.Empty<TestCase>();

            var testCases = new List<TestCase>();

            foreach (var i in testCasesRoot.ChildNodes)
            {
                var directives = new HashSet<string>(StringComparer.Ordinal);

                var directives_opt = i.FindChild("directives_opt");
                if (directives_opt != null)
                {
                    foreach (var j in directives_opt.ChildNodes)
                    {
                        directives.Add(j.FindChild("Identifier").FindTokenAndGetText());
                    }
                }

                testCases.Add(new TestCase
                {
                    Tree = i,
                    Directives = directives
                });
            }

            return testCases;
        }

        sealed class TestCase
        {
            public ParseTreeNode Tree { get; set; }
            public ISet<string> Directives { get; set; }
        }

        sealed class TestType
        {
            public string Namespace { get; set; }
            public string Name { get; set; }
            public string CSharpName { get; set; }
            public string BodyText { get; set; }
            public IUavcanType Body { get; set; }
            public IEnumerable<TestCase> TestCases { get; set; }
        }
    }
}
