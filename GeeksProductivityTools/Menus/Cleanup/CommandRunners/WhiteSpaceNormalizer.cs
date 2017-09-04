using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    // TODO: Refactor this bloody class Ali
    public class WhiteSpaceNormalizer : ICodeCleaner
    {
        const int MinLeftPadding = 4;
        const string WhiteSpaceTriviaText = "    ";

        public void Run(ProjectItem item) => Task.Run(() => NormalizeUsingDirectives(item));

        static void NormalizeUsingDirectives(ProjectItem item)
        {
            var file = item.ToFullPathPropertyValue();

            var initialSource = item.ToSyntaxNode();

            var newRoot = new Rewriter().Visit(initialSource);

            newRoot.WriteSourceTo(item.ToFullPathPropertyValue());
        }

        private class Rewriter : CSharpSyntaxRewriter
        {
            public override bool VisitIntoStructuredTrivia
            {
                get { return true; }
            }

            public override SyntaxTriviaList VisitList(SyntaxTriviaList list)
            {
                list = base.VisitList(list);

                var lineBreaksAtBeginning = list.TakeWhile(t => t.IsKind(SyntaxKind.EndOfLineTrivia)).Count();
                if (lineBreaksAtBeginning > 1)
                {
                    list = SyntaxFactory.TriviaList(list.Skip(lineBreaksAtBeginning - 1));
                }

                return list;
            }
        }
    }
}
