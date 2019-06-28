using Irony.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp.Testing.Framework
{
    public static class ParseTreeNodeExtensions
    {
        public static string GetText(this ParseTreeNode node, string source)
        {
            var span = node.Span;
            return source.Substring(span.Location.Position, span.Length);
        }

        public static ParseTreeNode FindChild(this ParseTreeNode node, string termName)
        {
            foreach (var i in node.ChildNodes)
            {
                if (i.Term?.Name == termName)
                    return i;

                var child = FindChild(i, termName);
                if (child != null)
                    return child;
            }

            return null;
        }
    }
}
