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
    public class WhiteSpaceNormalizer : CodeCleanerCommandRunnerBase, ICodeCleaner
    {
        public override SyntaxNode CleanUp(SyntaxNode initialSourceNode)
        {
            return NormalizeWhiteSpaceHelper(initialSourceNode);
        }

        public static SyntaxNode NormalizeWhiteSpaceHelper(SyntaxNode initialSourceNode)
        {
            if (GeeksProductivityToolsPackage.Instance != null)
            {
                initialSourceNode = Formatter.Format(initialSourceNode, GeeksProductivityToolsPackage.Instance.VsWorkspace);
            }
            initialSourceNode = new Rewriter(initialSourceNode).Visit(initialSourceNode);
            return initialSourceNode;
        }

        class Rewriter : CSharpSyntaxRewriter
        {
            bool _lastTokenIsAOpenBrace = false;
            bool _lastTokenIsACloseBrace = false;
            SyntaxKind _lastSpecialSyntax = SyntaxKind.None;
            MethodDeclarationSyntax _lastMthodDeclarationNode = null;
            SyntaxTrivia _endOfLineTrivia = default(SyntaxTrivia);

            public Rewriter(SyntaxNode initialSource) : base()
            {
                _endOfLineTrivia =
                    initialSource
                        .SyntaxTree
                        .GetRoot()
                        .DescendantTrivia(descendIntoTrivia: true)
                        .FirstOrDefault(x => x.IsKind(SyntaxKind.EndOfLineTrivia));
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                if (node == null) return base.Visit(node);

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
                else if (node is MethodDeclarationSyntax && node.Parent is ClassDeclarationSyntax)
                {
                    node = CheckMethodDeclaration(node);
                }
                else if (_lastMthodDeclarationNode != null && (_lastMthodDeclarationNode.DescendantNodes().Contains(node)) == false)
                {
                    _lastSpecialSyntax = SyntaxKind.None;
                    _lastMthodDeclarationNode = null;
                }

                return base.Visit(node);
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                if (_lastTokenIsACloseBrace)
                {
                    if (!(token.Parent is CatchClauseSyntax || token.Parent is ElseClauseSyntax || token.ValueText == ";"))
                    {
                        token = token.WithLeadingTrivia(token.LeadingTrivia.Insert(0, _endOfLineTrivia));
                    }
                    _lastTokenIsACloseBrace = false;
                }
                else if (_lastTokenIsAOpenBrace)
                {
                    var oldList = token.LeadingTrivia.ToList();
                    var newList = CleanUpOpenBrace(oldList);
                    token = token.WithLeadingTrivia(newList);
                    _lastTokenIsAOpenBrace = false;
                }

                if (token.IsKind(SyntaxKind.OpenBraceToken))
                {
                    _lastTokenIsAOpenBrace = true;
                }
                else if (token.IsKind(SyntaxKind.CloseBraceToken))
                {
                    var oldList = token.LeadingTrivia.ToList();
                    var newList = CleanUpCloseBrace(oldList);
                    if (CheckForAddBlankAfterBracesInsideMethods(token.Parent))
                    {
                        _lastTokenIsACloseBrace = true;
                    }
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
            bool CheckForAddBlankAfterBracesInsideMethods(SyntaxNode node)
            {
                if (node == null) return true;
                if (node is BlockSyntax == false) return false;
                if (node.Parent is DoStatementSyntax) return false;
                if (node.Parent is MethodDeclarationSyntax) return false;
                if (node.Parent is NamespaceDeclarationSyntax) return false;
                if (node.Parent is ParenthesizedLambdaExpressionSyntax) return false;
                if (node.Parent is ParenthesizedExpressionSyntax) return false;
                if (node.Parent is AnonymousMethodExpressionSyntax) return false;
                if (node.Parent is AnonymousFunctionExpressionSyntax) return false;
                if (node.Parent is TryStatementSyntax) return false;
                if (node.Parent is IfStatementSyntax && ((IfStatementSyntax)node.Parent).Else != null) return false;

                return true;
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

            SyntaxNode CheckMethodDeclaration(SyntaxNode node)
            {
                if (_lastMthodDeclarationNode != null)
                {
                    var leadingTrivia = CleanUpList(node.GetLeadingTrivia().ToList(), 1);

                    node = node.WithLeadingTrivia(leadingTrivia);
                }

                _lastMthodDeclarationNode = node as MethodDeclarationSyntax;

                return node;
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
                    for (var i = lineBreaksAtBeginning; i <= exactNumberOfBlanks; i++)
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

            IList<SyntaxTrivia> ProcessSpecialTrivias(IList<SyntaxTrivia> syntaxTrivias, bool itsForCloseBrace)
            {
                if (CheckShortSyntax(syntaxTrivias, itsForCloseBrace)) return syntaxTrivias;
                var specialTriviasCount =
                    syntaxTrivias
                        .Count(t =>
                            !t.IsKind(SyntaxKind.EndOfLineTrivia) && !t.IsKind(SyntaxKind.WhitespaceTrivia)
                        );

                var outputTriviasList = new List<SyntaxTrivia>();
                var specialTiviasCount = 0;
                var bAddedBlankLine = false;

                for (var i = 0; i < syntaxTrivias.Count; i++)
                {
                    var countOfChars = 0;

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

            bool CheckShortSyntax(IList<SyntaxTrivia> syntaxTrivias, bool itsForCloseBrace)
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