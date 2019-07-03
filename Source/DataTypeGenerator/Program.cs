using CanardSharp.Dsdl;
using CanardSharp.Dsdl.DataTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTypeGenerator
{
    class Program
    {
        static string GetRootDirectory()
        {
            var assembly = typeof(Program).Assembly;
            var rootPath = Path.GetDirectoryName(new Uri(assembly.EscapedCodeBase).LocalPath);
            while (rootPath != null)
            {
                var dsdlDefinitionsPath = Path.Combine(rootPath, "DsdlDefinitions");
                if (Directory.Exists(dsdlDefinitionsPath))
                {
                    return dsdlDefinitionsPath;
                }

                rootPath = Path.GetDirectoryName(rootPath);
            }

            throw new InvalidOperationException();
        }

        static void Main(string[] args)
        {
            var types = LoadTypes().ToList();

            foreach (var t in types)
            {
                _namesTypesLookup[t] = CreateCSharpName(t);
            }

            foreach (var t in types)
            {
                var content = BuildType(t, _namesTypesLookup[t]);
                Console.WriteLine(content);
            }
        }

        static string CreateCSharpName(IUavcanType t)
        {
            var name = t.Meta.Name;
            name = Capitalize(name);
            return name;
        }

        static string Capitalize(string name)
        {
            return name.Substring(0, 1).ToUpper() + name.Substring(1);
        }

        static Dictionary<object, string> _namesTypesLookup = new Dictionary<object, string>();

        static IEnumerable<IUavcanType> LoadTypes()
        {
            var root = Path.Combine(GetRootDirectory(), "uavcan");
            var resolver = new FileSystemUavcanTypeResolver(root);
            foreach (var i in Directory.EnumerateFiles(root, "*.uavcan", SearchOption.AllDirectories))
            {
                var meta = resolver.ParseMeta(i);
                yield return resolver.ResolveType(meta.Namespace, meta.Name);
            }
        }

        static string BuildType(IUavcanType t, string csharpName)
        {
            var builder = new StringBuilder();

            switch (t)
            {
                case MessageType mt:
                    builder.AppendLine($"    [DataContract(Name = \"{t.Meta.Name}\", Namespace = \"{t.Meta.Namespace}\")]");
                    builder.AppendLine($"    sealed class {csharpName}");
                    builder.AppendLine("    {");
                    foreach (var i in BuildCSharpMembers(mt))
                        builder.AppendLine("        " + i);
                    builder.AppendLine("    }");
                    builder.AppendLine();
                    break;

                case ServiceType st:
                    builder.AppendLine($"    [DataContract(Name = \"{t.Meta.Name}.Request\", Namespace = \"{t.Meta.Namespace}\")]");
                    builder.AppendLine($"    sealed class {csharpName}_Request");
                    builder.AppendLine("    {");
                    foreach (var i in BuildCSharpMembers(st.Request))
                        builder.AppendLine("        " + i);
                    builder.AppendLine("    }");
                    builder.AppendLine();

                    builder.AppendLine($"    [DataContract(Name = \"{t.Meta.Name}.Response\", Namespace = \"{t.Meta.Namespace}\")]");
                    builder.AppendLine($"    sealed class {csharpName}_Response");
                    builder.AppendLine("    {");
                    foreach (var i in BuildCSharpMembers(st.Request))
                        builder.AppendLine("        " + i);
                    builder.AppendLine("    }");
                    builder.AppendLine();
                    break;

                default:
                    throw new InvalidOperationException();
            }

            return builder.ToString();
        }

        static IEnumerable<string> BuildCSharpMembers(CompositeDsdlTypeBase type)
        {
            foreach (var m in type.Fields)
            {
                var fieldType = GetCSharpType(m.Type, type.IsUnion);
                if (fieldType != null)
                {
                    yield return $"[DataMember(Name = \"{m.Name}\")]";
                    yield return $"public {fieldType} {GetCSharpPropertyName(m.Name)} {{ get; set; }}";
                }
            }
        }

        static object GetCSharpPropertyName(string name)
        {
            var parts = name.Split('_');
            return string.Join("", parts.Select(Capitalize));
        }

        static string GetCSharpType(DsdlType type, bool nullable)
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
                    return _namesTypesLookup[t];

                default:
                    throw new InvalidOperationException();
            }
        }

        static string GetCSharpType(PrimitiveDsdlType type)
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
    }
}
