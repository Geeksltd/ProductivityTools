using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    // TODO: Refactor this bloody class Ali
    public class WhiteSpaceNormalizer : ICodeCleaner
    {
        public void Run(ProjectItem item) => Task.Run(() => NormalizeWhiteSpace(item));

        static void NormalizeWhiteSpace(ProjectItem item)
        {
            var initialSource = item.ToSyntaxNode();

            initialSource = new Rewriter().Visit(initialSource);

            initialSource.WriteSourceTo(item.ToFullPathPropertyValue());

            //var childs = initialSource.ChildNodes().ToList();
            //var newList = new SyntaxNodeOrTokenList();
            //for (int i = 0; i < childs.Count; i++)
            //{
            //    var syntaxNode = childs[i];
            //    var newRoot0 = new Rewriter().Visit(syntaxNode);
            //    if (i + 1 < childs.Count && !childs[i + 1].IsKind(SyntaxKind.UsingDirective))
            //    {
            //        newRoot0 = newRoot0.InsertTriviaAfter(newRoot0.DescendantTrivia(descendIntoTrivia: true).Last(),
            //            new SyntaxTrivia[]
            //            {
            //                initialSource.DescendantTrivia().First(x => x.IsKind(SyntaxKind.EndOfLineTrivia)),
            //            }
            //        );
            //    }
            //    newList.Add(newRoot0);
            //    initialSource = initialSource.ReplaceNode(syntaxNode, newRoot0);
            //}
            //new SyntaxNodeOrToken().
            //    initialSource.WriteSourceTo(item.ToFullPathPropertyValue());
        }

        class Rewriter : CSharpSyntaxRewriter
        {
            bool _lastTokenIsAOpenBrace = false;

            public override SyntaxTriviaList VisitList(SyntaxTriviaList list)
            {
                list = base.VisitList(list);

                if (list.Count == 1)
                {
                    _lastTokenIsAOpenBrace = list[0].Token.IsKind(SyntaxKind.OpenBraceToken);
                    return list;
                }

                var newList = list.ToList();

                var searchedComments = FindSpecialTrivias(newList);

                if (newList.First().Token.IsKind(SyntaxKind.CloseBraceToken))
                {
                    return SyntaxFactory.TriviaList(ProcessSpecialTrivias(newList, searchedComments, itsForCloseBrace: true));
                }
                if (_lastTokenIsAOpenBrace)
                {
                    var lineBreaksAtBeginning = newList.TakeWhile(t => t.IsKind(SyntaxKind.EndOfLineTrivia)).Count();

                    newList = newList.Skip(lineBreaksAtBeginning).ToList();

                    _lastTokenIsAOpenBrace = false;
                }

                newList = ProcessSpecialTrivias(newList, searchedComments, itsForCloseBrace: false);

                list = SyntaxFactory.TriviaList(newList);
                return list;
            }

            List<SyntaxTrivia> FindSpecialTrivias(IEnumerable<SyntaxTrivia> newList)
            {
                return
                    newList

                        .Where(t =>
                                t.IsKind(SyntaxKind.RegionDirectiveTrivia) ||
                                t.IsKind(SyntaxKind.EndRegionDirectiveTrivia) ||
                                t.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                                t.IsKind(SyntaxKind.MultiLineCommentTrivia)
                        )
                        .ToList();
            }

            bool RemoveBlankDuplication(ref List<SyntaxTrivia> newList, SyntaxKind kind, int iterationIndex)
            {
                if (iterationIndex >= newList.Count) return false;

                var lineBreaksAtBeginning = newList.Skip(iterationIndex).TakeWhile(t => t.IsKind(kind)).Count();

                if (lineBreaksAtBeginning > 1)
                {
                    newList.RemoveRange(iterationIndex, lineBreaksAtBeginning - 1);
                }
                return lineBreaksAtBeginning > 0;
            }
            List<SyntaxTrivia> ProcessSpecialTrivias(IList<SyntaxTrivia> syntaxTrivias, IList<SyntaxTrivia> searchedComments, bool itsForCloseBrace)
            {
                var newList = syntaxTrivias.ToList();

                var outputTriviasList = new List<SyntaxTrivia>();

                int specialTiviasCount = 0;

                for (int i = 0; i < newList.Count; i++)
                {
                    if (specialTiviasCount == searchedComments.Count)
                    {
                        if (itsForCloseBrace)
                        {
                            if (RemoveBlankDuplication(ref newList, SyntaxKind.EndOfLineTrivia, i))
                            {
                                i++;
                            }
                            if (RemoveBlankDuplication(ref newList, SyntaxKind.WhitespaceTrivia, i))
                            {
                                outputTriviasList.Add(newList[i]);
                            }
                            i = newList.Count;
                            continue;
                        }
                    }
                    if
                    (
                        (
                            newList[i].IsKind(SyntaxKind.EndOfLineTrivia) ||
                            newList[i].IsKind(SyntaxKind.WhitespaceTrivia) ||
                            newList[i].IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                            newList[i].IsKind(SyntaxKind.MultiLineCommentTrivia)
                        ) == false
                    )
                    {
                        outputTriviasList.Add(newList[i]);
                        specialTiviasCount++;
                        continue;
                    }

                    if (newList[i].IsKind(SyntaxKind.SingleLineCommentTrivia) || newList[i].IsKind(SyntaxKind.MultiLineCommentTrivia))
                    {
                        outputTriviasList.Add(newList[i]);
                        i++;
                        if (newList[i].IsKind(SyntaxKind.EndOfLineTrivia))
                        {
                            outputTriviasList.Add(newList[i]);
                        }
                        specialTiviasCount++;
                        continue;
                    }

                    if (RemoveBlankDuplication(ref newList, SyntaxKind.EndOfLineTrivia, i))
                    {
                        outputTriviasList.Add(newList[i]);
                        continue;
                    }
                    if (RemoveBlankDuplication(ref newList, SyntaxKind.WhitespaceTrivia, i))
                    {
                        outputTriviasList.Add(newList[i]);
                    }
                }
                return outputTriviasList;
            }
        }
    }
}
