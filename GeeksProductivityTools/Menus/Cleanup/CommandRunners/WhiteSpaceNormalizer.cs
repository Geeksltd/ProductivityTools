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
            initialSourceNode = new Rewriter2(initialSourceNode).Visit(initialSourceNode);
            return initialSourceNode;
        }

        static SyntaxTrivia _endOfLineTrivia = default(SyntaxTrivia);

        class Rewriter2 : CSharpSyntaxRewriter
        {
            SyntaxToken _lastToken = default(SyntaxToken);
            MemberDeclarationSyntax _LastMember = null;
            bool _lastTokenIsAOpenBrace = false;
            bool _lastTokenIsACloseBrace = false;

            public Rewriter2(SyntaxNode initialSource) : base()
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

                var triviList = node.GetLeadingTrivia();

                if (node is UsingDirectiveSyntax)
                {
                    triviList = CleanUpListWithoutNo(triviList);
                    _LastMember = null;
                }
                else if (node is NamespaceDeclarationSyntax)
                {
                    var namespaceNode = node as NamespaceDeclarationSyntax;

                    node =
                        namespaceNode
                            .WithOpenBraceToken(
                                namespaceNode.OpenBraceToken
                                    .WithLeadingTrivia(CleanUpListWithoutNo(namespaceNode.OpenBraceToken.LeadingTrivia)))
                            .WithCloseBraceToken(
                                namespaceNode.CloseBraceToken
                                    .WithLeadingTrivia(CleanUpListWithoutNo(namespaceNode.CloseBraceToken.LeadingTrivia, itsForCloseBrace: true))
                            );

                    var zeroCondition = _lastTokenIsAOpenBrace || _lastToken == default(SyntaxToken);
                    triviList = CleanUpList(triviList, zeroCondition ? 0 : 1);
                    _LastMember = null;
                }
                else if (node is ClassDeclarationSyntax)
                {
                    var classNode = node as ClassDeclarationSyntax;

                    node =
                        classNode
                            .WithOpenBraceToken(
                                classNode.OpenBraceToken
                                    .WithLeadingTrivia(CleanUpListWithoutNo(classNode.OpenBraceToken.LeadingTrivia)))
                            .WithCloseBraceToken(
                                classNode.CloseBraceToken
                                    .WithLeadingTrivia(CleanUpListWithoutNo(classNode.CloseBraceToken.LeadingTrivia, itsForCloseBrace: true))
                            );


                    var zeroCondition = _lastTokenIsAOpenBrace || _lastToken == default(SyntaxToken);
                    triviList = CleanUpList(triviList, zeroCondition ? 0 : 1);
                    _LastMember = null;
                }
                else if (node is StructDeclarationSyntax)
                {
                    var classNode = node as StructDeclarationSyntax;

                    node =
                        classNode
                            .WithOpenBraceToken(
                                classNode.OpenBraceToken
                                    .WithLeadingTrivia(CleanUpListWithoutNo(classNode.OpenBraceToken.LeadingTrivia)))
                            .WithCloseBraceToken(
                                classNode.CloseBraceToken
                                    .WithLeadingTrivia(CleanUpListWithoutNo(classNode.CloseBraceToken.LeadingTrivia, itsForCloseBrace: true))
                            );


                    var zeroCondition = _lastTokenIsAOpenBrace || _lastToken == default(SyntaxToken);
                    triviList = CleanUpList(triviList, zeroCondition ? 0 : 1);
                    _LastMember = null;
                }
                else if (node is MethodDeclarationSyntax && node.Parent is ClassDeclarationSyntax)
                {
                    if (_lastTokenIsAOpenBrace)
                    {
                        triviList = CleanUpListWith(triviList);
                    }
                    else if (_LastMember is MethodDeclarationSyntax)
                    {
                        triviList = CleanUpListWithExact(triviList, 1);
                    }
                    else
                    {
                        triviList = CleanUpListWith(triviList);
                    }
                    _LastMember = node as MethodDeclarationSyntax;
                }
                else if (node is MemberDeclarationSyntax)
                {
                    triviList = CleanUpListWith(triviList);
                    _LastMember = node as MemberDeclarationSyntax;
                }
                else if (node is BlockSyntax)
                {
                    var block = node as BlockSyntax;

                    node =
                        block
                            .WithOpenBraceToken(block.OpenBraceToken)
                            .WithStatements(MakeUpStatements(block.Statements))
                            .WithCloseBraceToken(
                                block.CloseBraceToken
                                    .WithLeadingTrivia(CleanUpListWithoutNo(block.CloseBraceToken.LeadingTrivia, itsForCloseBrace: true))
                            );
                }
                else if (node is StatementSyntax)
                {
                    var zeroCondition = _lastTokenIsAOpenBrace || _lastToken == default(SyntaxToken);
                    if (zeroCondition)
                    {
                        triviList = CleanUpListWithExact(triviList, 0);
                    }
                    else if (_lastTokenIsACloseBrace)
                    {
                        triviList = CleanUpListWithExact(triviList, 1, itsForCloseBrace: false);
                    }
                    else
                    {
                        triviList = CleanUpListWith(triviList);
                    }
                }
                else if (CheckBlocks(node))
                {
                    triviList = CleanUpListWith(triviList);
                }

                node = node.WithLeadingTrivia(triviList);

                return base.Visit(node);
            }

            bool CheckBlocks(SyntaxNode node)
            {
                if (node is CatchClauseSyntax) return true;
                if (node is FinallyClauseSyntax) return true;
                if (node is ElseClauseSyntax) return true;

                return false;
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                if (default(SyntaxToken) == token) return base.VisitToken(token);

                _lastToken = token;

                var tokenKind = token.Kind();

                _lastTokenIsAOpenBrace = tokenKind == SyntaxKind.OpenBraceToken;
                _lastTokenIsACloseBrace = false;
                var leadingTriviList = token.LeadingTrivia;

                if (tokenKind == SyntaxKind.CloseBraceToken)
                {
                    var triviasBetweenTokens = token.GetPreviousToken().TrailingTrivia.AddRange(token.LeadingTrivia);
                    if (triviasBetweenTokens.Any(x => x.IsKind(SyntaxKind.EndOfLineTrivia)))
                    {
                        _lastTokenIsACloseBrace = true;
                    }
                }
                else if (token.IsKind(SyntaxKind.EndOfFileToken))
                {
                    leadingTriviList = ProcessSpecialTrivias(CleanUpList(leadingTriviList, 0), itsForCloseBrace: false);
                    if (token.LeadingTrivia != leadingTriviList)
                    {
                        token = token.WithLeadingTrivia(leadingTriviList);
                    }
                }

                return base.VisitToken(token);
            }

            SyntaxList<StatementSyntax> MakeUpStatements(SyntaxList<StatementSyntax> blockStatements)
            {
                if (blockStatements.Any() == false) return blockStatements;
                var first = blockStatements[0];
                var newFirst = first.WithLeadingTrivia(CleanUpListWithExact(first.GetLeadingTrivia(), 0));
                return blockStatements.Replace(first, newFirst);
            }

            #region
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
                    syntaxTrivias = syntaxTrivias.Skip(lineBreaksAtBeginning - exactNumberOfBlanks)
                        .ToSyntaxTriviaList();
                }
                else if (lineBreaksAtBeginning < exactNumberOfBlanks)
                {
                    var newList = syntaxTrivias.ToList();
                    for (var i = lineBreaksAtBeginning; i < exactNumberOfBlanks; i++)
                    {
                        newList.Insert(0, _endOfLineTrivia);
                    }
                    syntaxTrivias = new SyntaxTriviaList().AddRange(newList);
                }

                return syntaxTrivias;
            }
            SyntaxTriviaList CleanUpListWithoutNo(SyntaxTriviaList syntaxTrivias, bool itsForCloseBrace = false)
            {
                syntaxTrivias = ProcessSpecialTrivias(syntaxTrivias, itsForCloseBrace);

                var specialTriviasCount =
                    syntaxTrivias
                        .Count(t =>
                            !t.IsKind(SyntaxKind.EndOfLineTrivia) && !t.IsKind(SyntaxKind.WhitespaceTrivia)
                        );

                if (specialTriviasCount > 0) return CleanUpList(syntaxTrivias);

                return CleanUpList(syntaxTrivias, 0);
            }

            SyntaxTriviaList CleanUpListWith(SyntaxTriviaList syntaxTrivias, bool itsForCloseBrace = false)
            {
                syntaxTrivias = CleanUpList(syntaxTrivias);
                syntaxTrivias = ProcessSpecialTrivias(syntaxTrivias, itsForCloseBrace);

                return syntaxTrivias;
            }
            SyntaxTriviaList CleanUpListWithExact(SyntaxTriviaList syntaxTrivias, int exactNumberOfBlanks, bool itsForCloseBrace = false)
            {
                syntaxTrivias = CleanUpList(syntaxTrivias, exactNumberOfBlanks);
                syntaxTrivias = ProcessSpecialTrivias(syntaxTrivias, itsForCloseBrace);

                return syntaxTrivias;
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

                    if (syntaxTrivias[i].IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                        syntaxTrivias[i].IsKind(SyntaxKind.MultiLineCommentTrivia))
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

                if (syntaxTrivias[0].IsKind(SyntaxKind.EndOfLineTrivia) &&
                    syntaxTrivias[1].IsKind(SyntaxKind.WhitespaceTrivia))
                    return true;
                if (syntaxTrivias[0].IsKind(SyntaxKind.WhitespaceTrivia) &&
                    syntaxTrivias[1].IsKind(SyntaxKind.EndOfLineTrivia))
                    return true;

                return false;
            }

            #endregion

        }
    }
}