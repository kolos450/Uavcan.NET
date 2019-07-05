using Irony.Parsing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp.Testing.Framework
{
    public class TestSchemeFileGrammar : Grammar
    {
        static NumberLiteral CreateNumberLiteral(string name)
        {
            NumberLiteral term = new NumberLiteral(name);
            term.DefaultIntTypes = new TypeCode[] { TypeCode.Int32, TypeCode.UInt32, TypeCode.Int64, TypeCode.UInt64 };
            term.DefaultFloatType = TypeCode.Double;
            term.AddPrefix("0x", NumberOptions.Hex);
            term.AddSuffix("u", TypeCode.UInt32, TypeCode.UInt64);
            term.AddSuffix("l", TypeCode.Int64, TypeCode.UInt64);
            term.AddSuffix("ul", TypeCode.UInt64);
            term.AddSuffix("f", TypeCode.Single);
            term.AddSuffix("d", TypeCode.Double);
            term.AddSuffix("m", TypeCode.Decimal);
            term.Options |= NumberOptions.AllowSign;
            return term;
        }

        public TestSchemeFileGrammar()
        {
            #region Lexical structure
            StringLiteral StringLiteral = TerminalFactory.CreateCSharpString("StringLiteral");
            NumberLiteral Number = CreateNumberLiteral("Number");
            IdentifierTerminal identifier = TerminalFactory.CreateCSharpIdentifier("Identifier");

            CommentTerminal SingleLineComment = new CommentTerminal("SingleLineComment", "//", "\r", "\n", "\u2085", "\u2028", "\u2029");
            CommentTerminal DelimitedComment = new CommentTerminal("DelimitedComment", "/*", "*/");
            NonGrammarTerminals.Add(SingleLineComment);
            NonGrammarTerminals.Add(DelimitedComment);
            //Temporarily, treat preprocessor instructions like comments
            CommentTerminal ppInstruction = new CommentTerminal("ppInstruction", "#", "\n");
            NonGrammarTerminals.Add(ppInstruction);

            //Symbols
            KeyTerm semi = ToTerm(";", "semi");
            NonTerminal semi_opt = new NonTerminal("semi?");
            semi_opt.Rule = Empty | semi;
            KeyTerm dot = ToTerm(".", "dot");
            KeyTerm comma = ToTerm(",", "comma");
            NonTerminal commas_opt = new NonTerminal("commas_opt");
            commas_opt.Rule = MakeStarRule(commas_opt, null, comma);
            KeyTerm Lbr = ToTerm("{");
            KeyTerm Rbr = ToTerm("}");
            #endregion

            #region NonTerminals
            var qual_name_with_targs = new NonTerminal("qual_name_with_targs");
            var qual_name_segment = new NonTerminal("qual_name_segment");
            var qual_name_segments_opt = new NonTerminal("qual_name_segments_opt");

            var compilation_unit = new NonTerminal("compilation_unit");
            var namespace_declaration = new NonTerminal("namespace_declaration");
            var namespace_declarations_opt = new NonTerminal("namespace_declarations_opt");
            var qualified_identifier = new NonTerminal("qualified_identifier");
            var namespace_body = new NonTerminal("namespace_body");
            var namespace_member_declaration = new NonTerminal("namespace_member_declaration");
            var namespace_member_declarations = new NonTerminal("namespace_member_declarations");
            var type_declaration = new NonTerminal("type_declaration");
            var type_body = new NonTerminal("type_body");
            var type_test_case = new NonTerminal("type_test_case");
            var type_test_cases_opt = new NonTerminal("type_test_cases_opt");
            var literal = new NonTerminal("literal");
            var member = new NonTerminal("member");
            var members = new NonTerminal("members");
            var nested_members = new NonTerminal("nested_members");
            var directive = new NonTerminal("directive");
            var directives_opt = new NonTerminal("directives_opt");

            var type_body_content = new FreeTextLiteral("type_body_content", FreeTextOptions.AllowEmpty, "}");
            #endregion

            #region operators, punctuation and delimiters

            this.MarkPunctuation(";", ",", "(", ")", "{", "}", "[", "]", ":");
            this.MarkTransient(namespace_member_declaration, literal);

            this.AddTermsReportGroup("assignment", "=", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=");
            this.AddTermsReportGroup("typename", "bool", "decimal", "float", "double", "string", "object",
              "sbyte", "byte", "short", "ushort", "int", "uint", "long", "ulong", "char");
            this.AddTermsReportGroup("statement", "if", "switch", "do", "while", "for", "foreach", "continue", "goto", "return", "try", "yield",
                                                  "break", "throw", "unchecked");
            this.AddTermsReportGroup("type declaration", "public", "private", "protected", "static", "internal", "sealed", "abstract", "partial",
                                                         "class");
            this.AddTermsReportGroup("member declaration", "virtual", "override", "readonly", "volatile", "extern");
            this.AddTermsReportGroup("constant", Number, StringLiteral);
            this.AddTermsReportGroup("constant", "true", "false", "null");

            this.AddTermsReportGroup("unary operator", "+", "-", "!", "~");

            this.AddToNoReportGroup(comma, semi);
            this.AddToNoReportGroup("var", "const", "new", "++", "--", "this", "base", "checked", "lock", "typeof", "default",
                                     "{", "}", "[");

            #endregion


            qual_name_segments_opt.Rule = MakeStarRule(qual_name_segments_opt, null, qual_name_segment);
            qual_name_segment.Rule = dot + identifier
                                    | "::" + identifier;
            qual_name_with_targs.Rule = identifier + qual_name_segments_opt;


            this.Root = compilation_unit;
            compilation_unit.Rule = namespace_declarations_opt;
            namespace_declaration.Rule = "namespace" + qualified_identifier + namespace_body + semi_opt;
            namespace_declarations_opt.Rule = MakeStarRule(namespace_declarations_opt, null, namespace_declaration);
            qualified_identifier.Rule = MakePlusRule(qualified_identifier, dot, identifier);

            namespace_body.Rule = "{" + namespace_member_declarations + "}";

            namespace_member_declaration.Rule = namespace_declaration | type_declaration;
            namespace_member_declarations.Rule = MakePlusRule(namespace_member_declarations, null, namespace_member_declaration);

            type_declaration.Rule = "type" + identifier + type_body + type_test_cases_opt;
            type_body.Rule = Lbr + type_body_content + Rbr;

            type_test_cases_opt.Rule = MakeStarRule(type_test_cases_opt, null, type_test_case);

            type_test_case.Rule = ".{" + members + "}" + directives_opt;

            members.Rule = MakeStarRule(members, ToTerm(","), member);

            member.Rule = literal | nested_members;

            nested_members.Rule = ("[" + members + "]") | ("{" + members + "}");

            literal.Rule = Number | StringLiteral | "true" | "false" | "null";

            directive.Rule = "@" + identifier;
            directives_opt.Rule = MakeStarRule(directives_opt, null, directive);
        }

        public override void SkipWhitespace(ISourceStream source)
        {
            while (!source.EOF())
            {
                var ch = source.PreviewChar;
                switch (ch)
                {
                    case ' ':
                    case '\t':
                    case '\r':
                    case '\n':
                    case '\v':
                    case '\u2085':
                    case '\u2028':
                    case '\u2029':
                        source.PreviewPosition++;
                        break;
                    default:
                        //Check unicode class Zs
                        UnicodeCategory chCat = char.GetUnicodeCategory(ch);
                        if (chCat == UnicodeCategory.SpaceSeparator) //it is whitespace, continue moving
                            continue;//while loop 
                                     //Otherwize return
                        return;
                }
            }
        }
    }
}
