using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public class CompactSmallIfElseStatements : CodeCleanerCommandRunnerBase, ICodeCleaner
    {
        public override SyntaxNode CleanUp(SyntaxNode initialSourceNode)
        {
            return CompactSmallIfElseStatementsHelper(initialSourceNode);
        }

        public static SyntaxNode CompactSmallIfElseStatementsHelper(SyntaxNode initialSourceNode)
        {
            initialSourceNode = new Rewriter(initialSourceNode).Visit(initialSourceNode);
            return initialSourceNode;
        }

        static SyntaxTrivia _endOfLineTrivia = default(SyntaxTrivia);
        const int MAX_IF_LINE_LENGTH = 50;

        class Rewriter : CSharpSyntaxRewriter
        {
            public Rewriter(SyntaxNode initialSource) : base()
            {
                _endOfLineTrivia =
                    initialSource
                        .SyntaxTree
                        .GetRoot()
                        .DescendantTrivia(descendIntoTrivia: true)
                        .FirstOrDefault(x => x.IsKind(SyntaxKind.EndOfLineTrivia));
            }

            public override SyntaxNode VisitIfStatement(IfStatementSyntax mainIFnode)
            {
                var newIfNode = Cleanup(mainIFnode);
                if (!string.Equals(newIfNode.ToFullString().Trim(), mainIFnode.ToFullString().Trim(), StringComparison.Ordinal))
                {
                    return newIfNode.WithTrailingTrivia(newIfNode.GetTrailingTrivia().Add(_endOfLineTrivia));
                }
                return mainIFnode;
            }


            SyntaxNode Cleanup(IfStatementSyntax originalIfNode)
            {
                if (CanCleanupIF(originalIfNode) == false) return base.VisitIfStatement(originalIfNode);

                StatementSyntax singleStatement;

                singleStatement = AnalyzeIfStatement(originalIfNode.Statement);
                if ((singleStatement == null || singleStatement is IfStatementSyntax) == false)
                {
                    originalIfNode = GetNewIF(originalIfNode, singleStatement);
                }


                if (originalIfNode.Else != null)
                {
                    var mainIFnode = originalIfNode;
                    singleStatement = AnalyzeIfStatement(mainIFnode.Else.Statement);
                    if (singleStatement != null)
                    {
                        //if (singleStatement is IfStatementSyntax ifSingleStatement)
                        //{
                        //    var singleStatementAsIf = Cleanup(ifSingleStatement);
                        //    mainIFnode = GetNewIfWithElse(mainIFnode, singleStatementAsIf);
                        //}
                        //else
                        //{
                        //    mainIFnode = GetNewIfWithElse(mainIFnode, singleStatement);
                        //}
                        if (singleStatement is IfStatementSyntax ifSingleStatement)
                        {
                            singleStatement = Cleanup(ifSingleStatement) as IfStatementSyntax;

                            //if (singleStatement != ifSingleStatement)
                            //{
                            //    mainIFnode = GetNewIfWithElse(mainIFnode, singleStatement);
                            //}
                        }

                        mainIFnode = GetNewIfWithElse(mainIFnode, singleStatement);

                        if (mainIFnode != originalIfNode)
                        {
                            originalIfNode = mainIFnode;
                        }
                    }
                }

                return originalIfNode;
            }

            private bool CanCleanupIF(IfStatementSyntax originalIfNode)
            {
                if (HasNoneWhitespaceTrivia(originalIfNode.DescendantTrivia(descendIntoTrivia: true))) return false;
                if (originalIfNode.ContainsDirectives) return false;

                return true;
                //if (originalIfNode.Condition.HasLeadingTrivia && HasNoneWhitespaceTrivia(originalIfNode.Condition.GetLeadingTrivia())) return false;
                //if (originalIfNode.Condition.HasTrailingTrivia && HasNoneWhitespaceTrivia(originalIfNode.Condition.GetTrailingTrivia())) return false;

                //if (originalIfNode.OpenParenToken.HasLeadingTrivia && HasNoneWhitespaceTrivia(originalIfNode.OpenParenToken.LeadingTrivia)) return false;
                //if (originalIfNode.OpenParenToken.HasTrailingTrivia && HasNoneWhitespaceTrivia(originalIfNode.OpenParenToken.TrailingTrivia)) return false;

                //if (originalIfNode.CloseParenToken.HasLeadingTrivia && HasNoneWhitespaceTrivia(originalIfNode.CloseParenToken.LeadingTrivia)) return false;
                //if (originalIfNode.CloseParenToken.HasTrailingTrivia && HasNoneWhitespaceTrivia(originalIfNode.CloseParenToken.TrailingTrivia)) return false;
            }

            IfStatementSyntax GetNewIF(IfStatementSyntax orginalIFnode, StatementSyntax singleStatement)
            {
                var newIf =
                    orginalIFnode
                        .WithIfKeyword(orginalIFnode.IfKeyword.WithTrailingTrivia(SyntaxFactory.Space))
                        .WithOpenParenToken(orginalIFnode.OpenParenToken.WithoutTrivia())
                        .WithCloseParenToken(orginalIFnode.CloseParenToken.WithoutTrivia())
                        .WithCondition(orginalIFnode.Condition.WithoutTrivia())
                        .WithStatement(
                            singleStatement
                                .WithLeadingTrivia(SyntaxFactory.Space)
                                .WithTrailingTrivia(_endOfLineTrivia)
                        );

                if (singleStatement is ReturnStatementSyntax returnStatement)
                {
                    if (returnStatement.Expression == null) return newIf;
                }
                if (singleStatement.DescendantNodes().OfType<BlockSyntax>().Any()) return orginalIFnode;
                if (newIf.WithElse(null).Span.Length > MAX_IF_LINE_LENGTH) return orginalIFnode;

                return newIf;
            }

            IfStatementSyntax GetNewIfWithElse(IfStatementSyntax originalIfNode, StatementSyntax singleStatement)
            {
                var newElse =
                    originalIfNode.Else
                        .WithElseKeyword(originalIfNode.Else.ElseKeyword.WithTrailingTrivia())
                        .WithoutTrailingTrivia()
                        .WithStatement(singleStatement.WithLeadingTrivia(SyntaxFactory.Space));

                if (singleStatement is ReturnStatementSyntax returnStatement)
                {
                    if (returnStatement.Expression == null) return originalIfNode.WithElse(newElse);
                }
                if (singleStatement is IfStatementSyntax)
                {
                    var newIf =
                            originalIfNode
                                .WithStatement(originalIfNode.Statement.WithoutTrailingTrivia())
                                .WithElse(newElse.WithLeadingTrivia(SyntaxFactory.Space));

                    if (newIf.Span.Length > MAX_IF_LINE_LENGTH) return originalIfNode;

                    return newIf;
                }
                if (singleStatement.DescendantNodes().OfType<BlockSyntax>().Any()) return originalIfNode;
                if (newElse.ElseKeyword.Span.Length + 1 + originalIfNode.WithElse(null).Span.Length > MAX_IF_LINE_LENGTH) return originalIfNode;

                return 
                    originalIfNode
                        .WithStatement(originalIfNode.Statement.WithoutTrailingTrivia())
                        .WithElse(newElse.WithLeadingTrivia(SyntaxFactory.Space));

            }

            StatementSyntax AnalyzeIfStatement(BlockSyntax block)
            {
                if (block.Statements.Count != 1) return null;
                if (block.ContainsDirectives) return null;
                if (block.HasLeadingTrivia && HasNoneWhitespaceTrivia(block.GetLeadingTrivia())) return null;
                if (HasNoneWhitespaceTrivia(block.OpenBraceToken.LeadingTrivia)) return null;
                if (HasNoneWhitespaceTrivia(block.OpenBraceToken.TrailingTrivia)) return null;
                return block.Statements.First();
            }

            StatementSyntax AnalyzeIfStatement(StatementSyntax singleStatement)
            {
                if (singleStatement is BlockSyntax newBlockStatement)
                {
                    if ((singleStatement = AnalyzeIfStatement(newBlockStatement)) == null) return null;
                }

                if (singleStatement.ContainsDirectives) return null;
                if (singleStatement.HasLeadingTrivia && HasNoneWhitespaceTrivia(singleStatement.GetLeadingTrivia())) return null;
                if (singleStatement.HasTrailingTrivia && HasNoneWhitespaceTrivia(singleStatement.GetTrailingTrivia())) return null;

                return singleStatement;
            }

            bool HasNoneWhitespaceTrivia(IEnumerable<SyntaxTrivia> triviaList)
            {
                return triviaList.Any(t => !t.IsKind(SyntaxKind.EndOfLineTrivia) && !t.IsKind(SyntaxKind.WhitespaceTrivia));
            }
        }
    }
}