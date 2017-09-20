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
                    var output = CleanUpList(node.GetLeadingTrivia(), 0);
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
                    var newList = CleanUpOpenBrace(token.LeadingTrivia);
                    token = token.WithLeadingTrivia(newList);
                }

                SyntaxTriviaList triviList = SyntaxTriviaList.Empty;

                if (token.IsKind(SyntaxKind.OpenBraceToken))
                {
                    if(token.Parent is NamespaceDeclarationSyntax || token.Parent is ClassDeclarationSyntax || (token.Parent is BlockSyntax && (token.Parent.Parent is MethodDeclarationSyntax)))
                    {
                        triviList = CleanUpList(token.LeadingTrivia, 0);
                    }
                    else
                    {
                        triviList = CleanUpList(token.LeadingTrivia);
                    }
                    _lastTokenIsAOpenBrace = true;
                    token = token.WithLeadingTrivia(triviList);
                }
                else if (token.IsKind(SyntaxKind.CloseBraceToken))
                {
                    triviList = CleanUpCloseBrace(token.LeadingTrivia);
                    var triviasBetweenTokens = token.GetPreviousToken().TrailingTrivia.AddRange(token.LeadingTrivia);
                    if (_lastTokenIsAOpenBrace || triviasBetweenTokens.Any(x => x.IsKind(SyntaxKind.EndOfLineTrivia)))
                    {
                        _lastTokenIsACloseBrace = CheckForAddBlankAfterBracesInsideMethods(token.Parent);
                    }
                    token = token.WithLeadingTrivia(triviList);
                    _lastTokenIsAOpenBrace = false;
                }
                else if (token.IsKind(SyntaxKind.EndOfFileToken))
                {
                    triviList = ProcessSpecialTrivias(CleanUpList(token.LeadingTrivia, 0), itsForCloseBrace: false);
                    token = token.WithLeadingTrivia(triviList);
                    _lastTokenIsAOpenBrace = false;
                }
                else if (token.LeadingTrivia.Count > 1)
                {
                    triviList = ProcessSpecialTrivias(CleanUpList(token.LeadingTrivia), itsForCloseBrace: false);
                    token = token.WithLeadingTrivia(triviList);
                    _lastTokenIsAOpenBrace = false;
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
                SyntaxTriviaList triviList;
                if (_lastSpecialSyntax == SyntaxKind.UsingKeyword)
                {
                    triviList = CleanUpListUsings(node.GetLeadingTrivia());
                }
                else if (_lastSpecialSyntax == SyntaxKind.None)
                {
                    triviList = CleanUpList(node.GetLeadingTrivia(), 0);
                }
                else
                {
                    triviList = CleanUpList(node.GetLeadingTrivia());
                }

                node = node.WithLeadingTrivia(triviList);

                _lastSpecialSyntax = syntaxNodeKind;

                return node;
            }

            SyntaxNode CheckMethodDeclaration(SyntaxNode node)
            {
                if (_lastMthodDeclarationNode != null)
                {
                    var leadingTrivia = CleanUpList(node.GetLeadingTrivia(), 1);

                    node = node.WithLeadingTrivia(leadingTrivia);
                }

                _lastMthodDeclarationNode = node as MethodDeclarationSyntax;

                return node;
            }
            SyntaxTriviaList CleanUpList(SyntaxTriviaList newList)
            {
                var lineBreaksAtBeginning = newList.TakeWhile(t => t.IsKind(SyntaxKind.EndOfLineTrivia)).Count();

                if (lineBreaksAtBeginning > 1)
                {
                    newList = newList.Skip(lineBreaksAtBeginning - 1).ToSyntaxTriviaList();
                }

                return newList;
            }

            SyntaxTriviaList CleanUpList(SyntaxTriviaList syntaxTrivias, int exactNumberOfBlanks)
            {
                var lineBreaksAtBeginning = syntaxTrivias.TakeWhile(t => t.IsKind(SyntaxKind.EndOfLineTrivia)).Count();

                if (lineBreaksAtBeginning > exactNumberOfBlanks)
                {
                    syntaxTrivias = syntaxTrivias.Skip(lineBreaksAtBeginning - exactNumberOfBlanks).ToSyntaxTriviaList();
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

            SyntaxTriviaList CleanUpListUsings(SyntaxTriviaList syntaxTrivias)
            {
                return CleanUpList(syntaxTrivias, 1);
            }

            SyntaxTriviaList CleanUpOpenBrace(SyntaxTriviaList syntaxTrivias)
            {
                return ProcessSpecialTrivias(CleanUpList(syntaxTrivias, 0), itsForCloseBrace: false);
            }

            SyntaxTriviaList CleanUpCloseBrace(SyntaxTriviaList syntaxTrivias)
            {
                return ProcessSpecialTrivias(CleanUpList(syntaxTrivias), itsForCloseBrace: true);
            }

            int RemoveBlankDuplication(SyntaxTriviaList syntaxTrivias, SyntaxKind kind, int iterationIndex)
            {
                if (iterationIndex >= syntaxTrivias.Count) return -1;

                var lineBreaksAtBeginning = syntaxTrivias.Skip(iterationIndex).TakeWhile(t => t.IsKind(kind)).Count();

                return lineBreaksAtBeginning - 1;
            }

            SyntaxTriviaList ProcessSpecialTrivias(SyntaxTriviaList syntaxTrivias, bool itsForCloseBrace)
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
                return outputTriviasList.ToSyntaxTriviaList();
            }

            bool CheckShortSyntax(SyntaxTriviaList syntaxTrivias, bool itsForCloseBrace)
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