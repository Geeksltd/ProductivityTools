using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    // TODO: Refactor this bloody class Ali
    public class WhiteSpaceNormalizer : ICodeCleaner
    {
        public void Run(ProjectItem item) => Task.Run(() => NormalizeWhiteSpace(item));

        static void NormalizeWhiteSpace(ProjectItem item)
        {
            var initialSource = item.ToSyntaxNode();

            initialSource = Formatter.Format(initialSource, GeeksProductivityToolsPackage.Instance.VsWorkspace);

            initialSource = RevomeDuplicaterBlank(initialSource);

            initialSource.WriteSourceTo(item.ToFullPathPropertyValue());
        }
        public static void NormalizeWhiteSpace(string address)
        {
            var initialSource = CSharpSyntaxTree.ParseText(File.ReadAllText(address)).GetRoot();

            initialSource = RevomeDuplicaterBlank(initialSource);

            initialSource.WriteSourceTo(address);
        }
        static SyntaxNode RevomeDuplicaterBlank(SyntaxNode initialSource)
        {
            initialSource = new Rewriter(initialSource).Visit(initialSource);
            return initialSource;
        }
        class Rewriter : CSharpSyntaxRewriter
        {
            bool _lastTokenIsAOpenBrace = false;
            private SyntaxTrivia _endOfLineTrivia = default(SyntaxTrivia);

            public Rewriter(SyntaxNode initialSource) : base()
            {
                _endOfLineTrivia =
                    initialSource
                        .SyntaxTree
                        .GetRoot()
                        .DescendantTrivia(descendIntoTrivia: true)
                        .FirstOrDefault(x => x.IsKind(SyntaxKind.EndOfLineTrivia));
            }

            private SyntaxKind _lastSpecialSyntax = SyntaxKind.None;
            public override SyntaxNode Visit(SyntaxNode node)
            {
                if (node is UsingDirectiveSyntax)
                {
                    var output = CleanUpList(node.GetLeadingTrivia().ToList(), 0);
                    node = node.WithLeadingTrivia(output);

                    _lastSpecialSyntax = SyntaxKind.UsingKeyword;
                }
                else if (node is NamespaceDeclarationSyntax)
                {
                    node = CheckSyntaxNodeAfterUsingNode(node, SyntaxKind.NamespaceKeyword);
                }
                else if (node is ClassDeclarationSyntax)
                {
                    node = CheckSyntaxNodeAfterUsingNode(node, SyntaxKind.ClassKeyword);
                }
                //else if (node != null)
                //{
                //    if (node.GetLeadingTrivia().Count > 1)
                //    {
                //        var output = CleanUpList(node.GetLeadingTrivia().ToList());
                //        output = ProcessSpecialTrivias(output, FindSpecialTriviasCount(output), itsForCloseBrace: false);
                //        node = node.WithLeadingTrivia(output);
                //    }
                //}
                return base.Visit(node);
            }

            SyntaxNode CheckSyntaxNodeAfterUsingNode(SyntaxNode node, SyntaxKind syntaxNodeKind)
            {
                if (_lastSpecialSyntax == SyntaxKind.UsingKeyword)
                {
                    var output = CleanUpListUsings(node.GetLeadingTrivia().ToList());
                    node = node.WithLeadingTrivia(output);
                }
                _lastSpecialSyntax = syntaxNodeKind;

                return node;
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                if (_lastTokenIsAOpenBrace)
                {
                    var oldList = token.LeadingTrivia.ToList();
                    var newList = CleanUpOpenBrace(oldList);
                    token = token.WithLeadingTrivia(newList);
                    _lastTokenIsAOpenBrace = false;
                }
                else if (token.IsKind(SyntaxKind.OpenBraceToken))
                {
                    _lastTokenIsAOpenBrace = true;
                }
                else if (token.IsKind(SyntaxKind.CloseBraceToken))
                {
                    var oldList = token.LeadingTrivia.ToList();
                    var newList = CleanUpCloseBrace(oldList);
                    token = token.WithLeadingTrivia(newList);
                }
                else if (token.LeadingTrivia.Count > 1)
                {
                    var output = CleanUpList(token.LeadingTrivia.ToList());
                    output = ProcessSpecialTrivias(output, itsForCloseBrace: false);
                    token = token.WithLeadingTrivia(output);
                }

                return base.VisitToken(token);
            }
            IList<SyntaxTrivia> CleanUpList(IList<SyntaxTrivia> newList)
            {
                var lineBreaksAtBeginning = newList.TakeWhile(t => t.IsKind(SyntaxKind.EndOfLineTrivia)).Count();

                if (lineBreaksAtBeginning > 1)
                {
                    newList = newList.Skip(lineBreaksAtBeginning - 1).ToList();
                }

                return newList;
            }
            IList<SyntaxTrivia> CleanUpList(IList<SyntaxTrivia> syntaxTrivias, int exactNumberOfBlanks)
            {
                var lineBreaksAtBeginning = syntaxTrivias.TakeWhile(t => t.IsKind(SyntaxKind.EndOfLineTrivia)).Count();

                if (lineBreaksAtBeginning > exactNumberOfBlanks)
                {
                    syntaxTrivias = syntaxTrivias.Skip(lineBreaksAtBeginning - exactNumberOfBlanks).ToList();
                }
                else if (lineBreaksAtBeginning < exactNumberOfBlanks)
                {
                    for (int i = lineBreaksAtBeginning; i <= exactNumberOfBlanks; i++)
                    {
                        syntaxTrivias.Insert(0, _endOfLineTrivia);
                    }
                }

                return syntaxTrivias;
            }
            IList<SyntaxTrivia> CleanUpListUsings(IList<SyntaxTrivia> syntaxTrivias)
            {
                return CleanUpList(syntaxTrivias, 1);
            }
            IList<SyntaxTrivia> CleanUpOpenBrace(IList<SyntaxTrivia> syntaxTrivias)
            {
                return ProcessSpecialTrivias(CleanUpList(syntaxTrivias, 0), itsForCloseBrace: false);
            }
            IList<SyntaxTrivia> CleanUpCloseBrace(IList<SyntaxTrivia> syntaxTrivias)
            {
                return ProcessSpecialTrivias(CleanUpList(syntaxTrivias), itsForCloseBrace: true);
            }
            int RemoveBlankDuplication(IList<SyntaxTrivia> syntaxTrivias, SyntaxKind kind, int iterationIndex)
            {
                if (iterationIndex >= syntaxTrivias.Count) return -1;

                var lineBreaksAtBeginning = syntaxTrivias.Skip(iterationIndex).TakeWhile(t => t.IsKind(kind)).Count();

                return lineBreaksAtBeginning - 1;
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
            IList<SyntaxTrivia> ProcessSpecialTrivias(IList<SyntaxTrivia> syntaxTrivias, bool itsForCloseBrace)
            {
                if (CheckShortSyntax(syntaxTrivias, itsForCloseBrace)) return syntaxTrivias;
                var specialTriviasCount = FindSpecialTriviasCount(syntaxTrivias);

                var outputTriviasList = new List<SyntaxTrivia>();
                int specialTiviasCount = 0;
                bool bAddedBlankLine = false;

                for (int i = 0; i < syntaxTrivias.Count; i++)
                {
                    int countOfChars = 0;

                    if (specialTiviasCount == specialTriviasCount)
                    {
                        if (itsForCloseBrace)
                        {
                            i += RemoveBlankDuplication(syntaxTrivias, SyntaxKind.EndOfLineTrivia, i) + 1;

                            if (RemoveBlankDuplication(syntaxTrivias, SyntaxKind.WhitespaceTrivia, i) != -1)
                            {
                                outputTriviasList.Add(syntaxTrivias[i]);
                            }
                            i = syntaxTrivias.Count;
                            continue;
                        }
                    }
                    if
                    (
                        (
                            syntaxTrivias[i].IsKind(SyntaxKind.EndOfLineTrivia) ||
                            syntaxTrivias[i].IsKind(SyntaxKind.WhitespaceTrivia) ||
                            syntaxTrivias[i].IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                            syntaxTrivias[i].IsKind(SyntaxKind.MultiLineCommentTrivia)
                        ) == false
                    )
                    {
                        outputTriviasList.Add(syntaxTrivias[i]);
                        specialTiviasCount++;
                        continue;
                    }

                    if (syntaxTrivias[i].IsKind(SyntaxKind.SingleLineCommentTrivia) || syntaxTrivias[i].IsKind(SyntaxKind.MultiLineCommentTrivia))
                    {
                        outputTriviasList.Add(syntaxTrivias[i]);
                        i++;
                        if (i < syntaxTrivias.Count && syntaxTrivias[i].IsKind(SyntaxKind.EndOfLineTrivia))
                        {
                            outputTriviasList.Add(syntaxTrivias[i]);
                        }
                        specialTiviasCount++;
                        continue;
                    }

                    if ((countOfChars = RemoveBlankDuplication(syntaxTrivias, SyntaxKind.EndOfLineTrivia, i)) != -1)
                    {
                        outputTriviasList.Add(syntaxTrivias[i]);
                        i += countOfChars + 1;
                        bAddedBlankLine = true;
                    }
                    if ((countOfChars = RemoveBlankDuplication(syntaxTrivias, SyntaxKind.WhitespaceTrivia, i)) != -1)
                    {
                        outputTriviasList.Add(syntaxTrivias[i]);
                        i += countOfChars;
                    }
                    else if (bAddedBlankLine)
                    {
                        i--;
                    }
                    bAddedBlankLine = false;
                }
                return outputTriviasList;
            }

            private bool CheckShortSyntax(IList<SyntaxTrivia> syntaxTrivias, bool itsForCloseBrace)
            {
                if (itsForCloseBrace) return false;
                if (syntaxTrivias.Count <= 1) return true;
                if (syntaxTrivias.Count > 2) return false;

                if (syntaxTrivias[0].IsKind(SyntaxKind.EndOfLineTrivia) && syntaxTrivias[1].IsKind(SyntaxKind.WhitespaceTrivia))
                    return true;
                if (syntaxTrivias[0].IsKind(SyntaxKind.WhitespaceTrivia) && syntaxTrivias[1].IsKind(SyntaxKind.EndOfLineTrivia))
                    return true;

                return false;
            }
        }
    }
}
