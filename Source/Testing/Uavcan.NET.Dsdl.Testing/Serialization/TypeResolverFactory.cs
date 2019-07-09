using Uavcan.NET.Testing.Framework;
using Irony.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET.Dsdl.Testing.Serialization
{
    sealed class TypeResolverFactory
    {
        List<TypeData> _types = new List<TypeData>();
        Stack<string> _namespacesStack = new Stack<string>();
        string _sourceText;

        sealed class TypeData
        {
            public string Body;
            public UavcanTypeMeta Meta;
        }

        public IUavcanTypeResolver Create(string testScheme)
        {
            _sourceText = testScheme;

            var grammar = new TestSchemeFileGrammar();
            var parser = new Parser(grammar);
            var tree = parser.Parse(testScheme);

            if (tree.Status != ParseTreeStatus.Parsed)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Cannot parse test scheme file.");

                foreach (var msg in tree.ParserMessages)
                {
                    sb.AppendLine($"{msg} at {msg.Location}.");
                }

                throw new InvalidOperationException(sb.ToString());
            }

            CollectTestTypes(tree.Root);

            return new StringUavcanTypeResolver(_types.Select(x => (x.Meta, x.Body)));
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
                    var namespaceName = node.FindChild("qualified_identifier").GetText(_sourceText);

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
                    CreateType(string.Join(".", _namespacesStack), typeName, typeBody);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        void CreateType(string ns, string name, string body)
        {
            var type = new TypeData
            {
                Meta = new UavcanTypeMeta
                {
                    Namespace = ns,
                    Name = name,
                },
                Body = body
            };

            _types.Add(type);
        }
    }
}
