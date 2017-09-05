using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.CSharp;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    // TODO: Refactor this bloody class Ali
    public class WhiteSpaceNormalizer : ICodeCleaner
    {
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
            private bool LastTokenIsAOpenBrace = false;
            public override SyntaxTriviaList VisitList(SyntaxTriviaList list)
            {
                list = base.VisitList(list);

                var newList = list.ToList();

                for (int i = 0; i < newList.Count; i++)
                {
                    var item = newList[i];

                    if (item.Token.IsKind(SyntaxKind.CloseBraceToken))
                    {
                        var searchedComments = getCommecnts(newList);

                        if (searchedComments.Any())
                        {
                            newList = ProcessWithComments(newList, searchedComments, true);
                            i = newList.Count;
                        }
                        else if (newList.Count > i + 1)
                        {
                            var lineBreaksAtBeginning =
                                newList.Skip(i).TakeWhile(t => t.IsKind(SyntaxKind.EndOfLineTrivia)).Count();

                            newList.RemoveRange(i, lineBreaksAtBeginning);
                        }
                    }
                    else if (item.IsKind(SyntaxKind.EndOfLineTrivia) || item.IsKind(SyntaxKind.WhitespaceTrivia))
                    {
                        var searchedComments = getCommecnts(newList);

                        if (searchedComments.Any())
                        {
                            if (LastTokenIsAOpenBrace)
                            {
                                var lineBreaksAtBeginning =
                                    newList.Skip(i).TakeWhile(t => t.IsKind(SyntaxKind.EndOfLineTrivia)).Count();

                                newList.RemoveRange(i, lineBreaksAtBeginning);

                                LastTokenIsAOpenBrace = false;
                            }

                            newList = ProcessWithComments(newList, searchedComments, false);
                            i = newList.Count;
                        }
                        else if (newList.Count > i + 1 && item.IsKind(SyntaxKind.EndOfLineTrivia))
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

            private List<SyntaxTrivia> getCommecnts(List<SyntaxTrivia> newList)
            {
                return
                    newList
                        .Where(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                                    t.IsKind(SyntaxKind.MultiLineCommentTrivia))
                        .ToList();
            }

            List<SyntaxTrivia> ProcessWithComments(List<SyntaxTrivia> syntaxTrivias, IList<SyntaxTrivia> searchedComments, bool itsForCloseBrace)
            {
                var newList = syntaxTrivias.ToList();

                var output = new List<SyntaxTrivia>();

                var comments = searchedComments;
                var firstLine = newList.FirstOrDefault(x => x.IsKind(SyntaxKind.EndOfLineTrivia));


                for (int i = 0; i < comments.Count + 1; i++)
                {
                    int arrayIndex = 0;

                    var selectedEnOfLines = newList.TakeWhile(x => x.IsKind(SyntaxKind.EndOfLineTrivia)).ToList();
                    if (selectedEnOfLines.Count != 0)
                    {
                        output.Add(selectedEnOfLines[0]);
                        if (itsForCloseBrace && i > 0 && i < comments.Count)
                        {
                            if (firstLine != null) output.Add(firstLine);
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
                        arrayIndex++;

                        if (!itsForCloseBrace)
                        {
                            if (firstLine != null)
                            {
                                output.Add(firstLine);
                                arrayIndex++;
                            }
                        }
                    }
                    newList.RemoveRange(0, arrayIndex);
                }
                return output;
            }
        }
    }
}
