using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

            initialSource = InsertABlankLineAfterLastUsing(new UsingRewriterBase(), initialSource);

            initialSource.WriteSourceTo(item.ToFullPathPropertyValue());
        }

        private static SyntaxNode InsertABlankLineAfterLastUsing(UsingRewriterBase usingRewriter, SyntaxNode initialSource)
        {
            var lastUsing = initialSource.ChildNodes().OfType<UsingDirectiveSyntax>().LastOrDefault();
            if (lastUsing != null)
            {
                var lastToken = lastUsing.ChildTokens().Last();
                var newToken = usingRewriter.VisitToken(lastToken);

                initialSource = initialSource.ReplaceToken(lastToken, newToken);
            }
            foreach (var namespaceDeclarationSyntax in initialSource.ChildNodes().OfType<NamespaceDeclarationSyntax>())
            {
                var newNameSpace = InsertABlankLineAfterLastUsing(new UsingRewriterBase(), namespaceDeclarationSyntax);
                initialSource = initialSource.ReplaceNode(namespaceDeclarationSyntax, newNameSpace);
            }
            return initialSource;
        }

        class UsingRewriterBase : CSharpSyntaxRewriter
        {
            public override SyntaxTriviaList VisitList(SyntaxTriviaList list) => FormatUsings(list, 2);

            protected SyntaxTriviaList FormatUsings(SyntaxTriviaList list, int limit)
            {
                list = base.VisitList(list);

                var lineBreaksAtBeginning = list.TakeWhile(t => t.IsKind(SyntaxKind.EndOfLineTrivia)).Count();
                var line = list.First().SyntaxTree.GetRoot().DescendantTrivia().First(x => x.IsKind(SyntaxKind.EndOfLineTrivia));
                if (lineBreaksAtBeginning < limit)
                {
                    list = list.Add(line);
                }

                return list;
            }
        }

        //class OutterUsingRewriter : UsingRewriterBase
        //{
        //    public override SyntaxTriviaList VisitList(SyntaxTriviaList list)
        //    {
        //        return FormatUsings(list, 2);
        //    }

        //}
        //class InnerUsingRewriter : UsingRewriterBase
        //{
        //    public override SyntaxTriviaList VisitList(SyntaxTriviaList list)
        //    {
        //        return FormatUsings(list, 2);
        //    }
        //}

        class Rewriter : CSharpSyntaxRewriter
        {
            bool _lastTokenIsAOpenBrace = false;
            bool _lastTokenIsACloseBrace = false;

            public override SyntaxTriviaList VisitList(SyntaxTriviaList list)
            {
                list = base.VisitList(list);

                if (list.Count == 1)
                {
                    if (list[0].Token.IsKind(SyntaxKind.NamespaceKeyword) && list[0].IsKind(SyntaxKind.EndOfLineTrivia))
                    {
                        return SyntaxFactory.TriviaList();
                    }

                    _lastTokenIsAOpenBrace = list[0].Token.IsKind(SyntaxKind.OpenBraceToken);
                    return list;
                }

                var newList = list.ToList();

                var specialTriviasCount = FindSpecialTriviasCount(newList);

                var firstTrivia = newList.First();

                if (firstTrivia.Token.IsKind(SyntaxKind.CloseBraceToken))
                {
                    _lastTokenIsACloseBrace = true;
                    return SyntaxFactory.TriviaList(ProcessSpecialTrivias(newList, specialTriviasCount, itsForCloseBrace: true));
                }

                if (_lastTokenIsAOpenBrace)
                {
                    newList = CleanUpList(newList);

                    _lastTokenIsAOpenBrace = false;
                }

                if
                (
                    !_lastTokenIsACloseBrace &&
                    (firstTrivia.Token.IsKind(SyntaxKind.NamespaceKeyword) || firstTrivia.Token.IsKind(SyntaxKind.ClassKeyword))
                )
                {
                    newList = CleanUpList(newList);
                }

                newList = ProcessSpecialTrivias(newList, specialTriviasCount, itsForCloseBrace: false);

                _lastTokenIsACloseBrace = false;

                list = SyntaxFactory.TriviaList(newList);
                return list;
            }

            private List<SyntaxTrivia> CleanUpList(IEnumerable<SyntaxTrivia> newList)
            {
                var lineBreaksAtBeginning = newList.TakeWhile(t => t.IsKind(SyntaxKind.EndOfLineTrivia)).Count();

                return newList.Skip(lineBreaksAtBeginning).ToList();
            }

            int FindSpecialTriviasCount(IEnumerable<SyntaxTrivia> newList)
            {
                return
                    newList

                        .Count(t =>
                            t.IsKind(SyntaxKind.RegionDirectiveTrivia) ||
                            t.IsKind(SyntaxKind.EndRegionDirectiveTrivia) ||
                            t.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                            t.IsKind(SyntaxKind.MultiLineCommentTrivia)
                        );
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
            List<SyntaxTrivia> ProcessSpecialTrivias(IList<SyntaxTrivia> syntaxTrivias, int specialTriviasCount, bool itsForCloseBrace)
            {
                var newList = syntaxTrivias.ToList();

                var outputTriviasList = new List<SyntaxTrivia>();

                int specialTiviasCount = 0;

                for (int i = 0; i < newList.Count; i++)
                {
                    if (specialTiviasCount == specialTriviasCount)
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
                        if (i < newList.Count && newList[i].IsKind(SyntaxKind.EndOfLineTrivia))
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
