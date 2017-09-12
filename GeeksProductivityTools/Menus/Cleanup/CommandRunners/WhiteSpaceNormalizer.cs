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
        //{
        //    NormalizeWhiteSpace(item);
        //    //Task.Run(() => NormalizeWhiteSpace(item));
        //}

        static void NormalizeWhiteSpace(ProjectItem item)
        {
            var initialSource = item.ToSyntaxNode();

            initialSource = Formatter.Format(initialSource, GeeksProductivityToolsPackage.Instance.VsWorkspace);

            initialSource = RevomeDuplicaterBlank(initialSource);//.NormalizeWhitespace();

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

                if (node is PropertyDeclarationSyntax)
                {
                    node = CheckForReadOnlyProperties(node as PropertyDeclarationSyntax);
                }

                return base.Visit(node);
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
                node = CheckMethodForSingleReturnStatement(node as MethodDeclarationSyntax);
                if (_lastMthodDeclarationNode != null)
                {
                    var leadingTrivia = CleanUpList(node.GetLeadingTrivia().ToList(), 1);

                    node = node.WithLeadingTrivia(leadingTrivia);
                }

                _lastMthodDeclarationNode = node as MethodDeclarationSyntax;

                return node;
            }

            static SyntaxTrivia[] _spaceTrivia = { SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, " ") };

            MethodDeclarationSyntax CheckMethodForSingleReturnStatement(MethodDeclarationSyntax methodDeclaration)
            {
                if ((methodDeclaration.ReturnType is PredefinedTypeSyntax) && (methodDeclaration.ReturnType as PredefinedTypeSyntax).Keyword.IsKind(SyntaxKind.VoidKeyword)) return methodDeclaration;
                if (methodDeclaration.Body == null) return methodDeclaration;
                if (methodDeclaration.Body.Statements.Count > 1) return methodDeclaration;
                var returnStatements = methodDeclaration.Body.Statements.OfType<ReturnStatementSyntax>();
                if (returnStatements.Count() != 1) return methodDeclaration;
                var expression = returnStatements.First().Expression.WithoutLeadingTrivia();
                var length =
                    expression.WithoutTrivia().Span.Length +
                    methodDeclaration.Span.Length -
                    methodDeclaration.Body.FullSpan.Length;

                if (length >= 100) return methodDeclaration;

                var closeParen = methodDeclaration.DescendantTokens().FirstOrDefault(x => x.IsKind(SyntaxKind.CloseParenToken));
                if (closeParen != null)
                {
                    methodDeclaration = methodDeclaration.ReplaceToken(closeParen, closeParen.WithTrailingTrivia(_spaceTrivia));
                }

                var newMethod =
                    methodDeclaration
                        .WithLeadingTrivia(methodDeclaration.GetLeadingTrivia())
                        .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(expression.WithLeadingTrivia(_spaceTrivia)))
                        .WithBody(null)
                        .WithSemicolonToken(GetSemicolon(methodDeclaration.Body))
                        .WithAdditionalAnnotations(Formatter.Annotation);


                return newMethod;
            }
            PropertyDeclarationSyntax CheckForReadOnlyProperties(PropertyDeclarationSyntax propertyDeclaration)
            {
                if (propertyDeclaration.AccessorList == null) return propertyDeclaration;
                var getNode = propertyDeclaration.AccessorList.Accessors.FirstOrDefault(x => x.Keyword.IsKind(SyntaxKind.GetKeyword));
                var setNode = propertyDeclaration.AccessorList.Accessors.FirstOrDefault(x => x.Keyword.IsKind(SyntaxKind.SetKeyword));
                if (setNode != null || getNode.Body == null) return propertyDeclaration;
                if (getNode.Body == null) return propertyDeclaration;
                if (getNode.Body.Statements.Count > 1) return propertyDeclaration;

                var returnStatements = getNode.Body.Statements.OfType<ReturnStatementSyntax>().ToList();
                if (returnStatements.Count() != 1) return propertyDeclaration;
                var expression = returnStatements.First().Expression.WithoutTrivia();

                var length =
                    expression.Span.Length +
                    propertyDeclaration.Span.Length -
                    propertyDeclaration.AccessorList.FullSpan.Length;

                if (length >= 100) return propertyDeclaration;

                propertyDeclaration =
                    propertyDeclaration
                        .WithIdentifier(propertyDeclaration.Identifier.WithTrailingTrivia(_spaceTrivia))
                        .WithLeadingTrivia(propertyDeclaration.GetLeadingTrivia())
                        .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(expression.WithLeadingTrivia(_spaceTrivia)))
                        .WithAccessorList(null)
                        .WithSemicolonToken(GetSemicolon(propertyDeclaration.AccessorList))
                        .WithAdditionalAnnotations(Formatter.Annotation);

                return propertyDeclaration;
            }

            SyntaxToken GetSemicolon(BlockSyntax block)
            {
                var semicolon = ((ReturnStatementSyntax)block.Statements[0]).SemicolonToken;

                var trivia = semicolon.TrailingTrivia.AsEnumerable();
                trivia = trivia.Where(t => !t.IsKind(SyntaxKind.EndOfLineTrivia));

                var closeBraceTrivia = block.CloseBraceToken.TrailingTrivia.AsEnumerable();
                trivia = trivia.Concat(closeBraceTrivia);

                return semicolon.WithTrailingTrivia(trivia);
            }
            SyntaxToken GetSemicolon(AccessorListSyntax accessorList)
            {
                var semicolon = ((ReturnStatementSyntax)accessorList.Accessors[0].Body.Statements[0]).SemicolonToken;

                var trivia = semicolon.TrailingTrivia.AsEnumerable();
                trivia = trivia.Where(t => !t.IsKind(SyntaxKind.EndOfLineTrivia));

                var closeBraceTrivia = accessorList.CloseBraceToken.TrailingTrivia.AsEnumerable();
                trivia = trivia.Concat(closeBraceTrivia);

                return semicolon.WithTrailingTrivia(trivia);
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