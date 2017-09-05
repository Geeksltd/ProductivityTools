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

            public override SyntaxNode Visit(SyntaxNode node)
            {
                return base.Visit(node);
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                return base.VisitToken(token);
            }

            public override SyntaxNode VisitSkippedTokensTrivia(SkippedTokensTriviaSyntax node)
            {
                return base.VisitSkippedTokensTrivia(node);
            }

            private bool LastTokenIsAOpenBrace = false;
            public override SyntaxTriviaList VisitList(SyntaxTriviaList list)
            {
                list = base.VisitList(list);

                var newList = list.ToList();

                for (int i = 0; i < newList.Count; i++)
                {
                    var item = newList[i];

                    if (item.IsKind(SyntaxKind.SingleLineCommentTrivia) || item.IsKind(SyntaxKind.MultiLineCommentTrivia))
                    {
                        LastTokenIsAOpenBrace = false;
                        if (newList.Count > i + 1)
                        {
                            if (newList[i + 1].IsKind(SyntaxKind.EndOfLineTrivia))
                            {
                                i++;
                            }
                        }
                    }
                    else if (item.Token.IsKind(SyntaxKind.CloseBraceToken))
                    {
                        var currentList = newList.Skip(i).ToList();

                        var comments =
                            currentList.Where(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                                                   t.IsKind(SyntaxKind.MultiLineCommentTrivia));

                        if (comments.Any())
                        {
                            newList = ProcessWithComments(newList);
                            i = newList.Count;
                        }
                        else if (newList.Count > i + 1)
                        {
                            var lineBreaksAtBeginning =
                                newList.Skip(i).TakeWhile(t => t.IsKind(SyntaxKind.EndOfLineTrivia)).Count();

                            newList.RemoveRange(i, lineBreaksAtBeginning);
                        }
                    }
                    else if (item.IsKind(SyntaxKind.EndOfLineTrivia))
                    {
                        if (newList.Count > i + 1)
                        {
                            if (LastTokenIsAOpenBrace)
                            {
                                var lineBreaksAtBeginning =
                                    newList.Skip(i).TakeWhile(t => t.IsKind(SyntaxKind.EndOfLineTrivia)).Count();

                                newList.RemoveRange(i, lineBreaksAtBeginning);

                                LastTokenIsAOpenBrace = false;
                            }
                            else if (newList[i + 1].IsKind(SyntaxKind.EndOfLineTrivia))
                            {
                                newList.RemoveAt(i + 1);
                                i--;
                            }
                        }
                    }
                }
                LastTokenIsAOpenBrace = false;
                if (list[0].Token.IsKind(SyntaxKind.OpenBraceToken))
                {
                    LastTokenIsAOpenBrace = true;
                }

                list = SyntaxFactory.TriviaList(newList);
                return list;
            }

            List<SyntaxTrivia> ProcessWithComments(List<SyntaxTrivia> list)
            {
                var newList = list.ToList();

                var output = new List<SyntaxTrivia>();

                var comments =
                    newList
                    .Where(
                        t =>
                            t.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                            t.IsKind(SyntaxKind.MultiLineCommentTrivia)
                    )
                    .ToList();

                for (int i = 0; i < comments.Count + 1; i++)
                {
                    int arrayIndex = 0;

                    var selectedEnOfLines = newList.TakeWhile(x => x.IsKind(SyntaxKind.EndOfLineTrivia)).ToList();
                    if (selectedEnOfLines.Count != 0)
                    {
                        output.Add(selectedEnOfLines[0]);
                        if (i > 0 && i < comments.Count)
                        {
                            output.Add(selectedEnOfLines[0]);
                        }
                        arrayIndex += selectedEnOfLines.Count;
                    }
                    var selectedWhiteSpaces = newList.Skip(arrayIndex).TakeWhile(x => x.IsKind(SyntaxKind.WhitespaceTrivia)).ToList();
                    if (selectedWhiteSpaces.Count != 0)
                    {
                        output.Add(selectedWhiteSpaces[0]);
                        arrayIndex += selectedWhiteSpaces.Count;
                    }
                    if (i < comments.Count)
                    {
                        output.Add(comments[i]);
                        arrayIndex += 1;
                    }
                    newList.RemoveRange(0, arrayIndex);
                }
                return output;
            }
        }
    }
}
