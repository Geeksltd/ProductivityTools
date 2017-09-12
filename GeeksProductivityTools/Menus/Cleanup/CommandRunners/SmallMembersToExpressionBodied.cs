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
    public class SmallMembersToExpressionBody : ICodeCleaner
    {
        public void Run(ProjectItem item) => Task.Run(() => ConvertSmallMembersToExpressionBody(item));

        static void ConvertSmallMembersToExpressionBody(ProjectItem item)
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

                if (node is MethodDeclarationSyntax && node.Parent is ClassDeclarationSyntax)
                {
                    node = ConvertMethodToExpressionBodied(node as MethodDeclarationSyntax);
                }
                else if (node is PropertyDeclarationSyntax)
                {
                    node = ConvertPropertyToExpressionBodied(node as PropertyDeclarationSyntax);
                }

                return base.Visit(node);
            }

            static SyntaxTrivia[] _spaceTrivia = { SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, " ") };
            MethodDeclarationSyntax ConvertMethodToExpressionBodied(MethodDeclarationSyntax methodDeclaration)
            {
                var expression = AnalyzeMethods(methodDeclaration);

                if (expression == null) return methodDeclaration;

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
            ExpressionSyntax AnalyzeMethods(MethodDeclarationSyntax method)
            {
                var classDeclaration = method.Parent as ClassDeclarationSyntax;

                if (classDeclaration == null) return null;
                ////if (classDeclaration?.As<ClassDeclarationSyntax>()?.Identifier.ValueText == "TaskManager") return false;
                //////var methodInfo = context.SemanticModel.GetDeclaredSymbol(method);
                if (method.Body == null) return null;
                if (method.Body.Statements.Count != 1) return null;
                //if (method.Body.ChildNodes().OfType<StatementSyntax>().IsSingle() == false) return;

                var singleStatement = method.Body.Statements.First();
                if (singleStatement is IfStatementSyntax) return null;
                if (singleStatement is ThrowStatementSyntax) return null;
                if (singleStatement is YieldStatementSyntax) return null;

                var expression =
                    (
                        (singleStatement is ReturnStatementSyntax) ?
                        (singleStatement as ReturnStatementSyntax).Expression
                        :
                        (singleStatement as ExpressionStatementSyntax).Expression
                    )
                    .WithoutLeadingTrivia();

                var length = expression.WithoutTrivia().Span.Length + method.Span.Length - method.Body.FullSpan.Length;
                if (length > 100) return null;
                if (method.Body.ChildNodes().OfType<UsingStatementSyntax>().Any()) return null;

                return expression;
            }

            PropertyDeclarationSyntax ConvertPropertyToExpressionBodied(PropertyDeclarationSyntax propertyDeclaration)
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
                var statement = block.Statements.First();

                var semicolon =
                    (statement is ExpressionStatementSyntax) ?
                    (statement as ExpressionStatementSyntax).SemicolonToken :
                    (statement as ReturnStatementSyntax).SemicolonToken;

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
        }
    }
}