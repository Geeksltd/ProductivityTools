using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public class RemoveExtraThis : CodeCleanerCommandRunnerBase, ICodeCleaner
    {
        public override SyntaxNode CleanUp(SyntaxNode initialSourceNode)
        {
            return new Rewriter(ProjectItemSemanticModel).Visit(initialSourceNode);
        }


        class Rewriter : CSharpSyntaxRewriter
        {
            private readonly SemanticModel semanticModel;

            public Rewriter(SemanticModel semanticModel)
            {
                this.semanticModel = semanticModel;
            }
            public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                return base.VisitClassDeclaration(Remove(node));
            }

            ClassDeclarationSyntax Remove(ClassDeclarationSyntax classNode)
            {
                var thises = classNode.DescendantNodes().OfType<MemberAccessExpressionSyntax>();

                Dictionary<MemberAccessExpressionSyntax, SyntaxNode> newItems = new Dictionary<MemberAccessExpressionSyntax, SyntaxNode>();

                foreach (var thisItem in thises)
                {
                    var symbol = semanticModel.GetSymbolInfo(thisItem.Name);
                    var symbol2 = semanticModel.GetSymbolInfo(thisItem);

                    if (symbol.Symbol == symbol2.Symbol && symbol.Symbol != null)
                    {
                        newItems.Add(thisItem, thisItem.Name.WithLeadingTrivia(thisItem.GetLeadingTrivia()));
                    }
                }

                if (newItems.Any())
                {
                    classNode = classNode.ReplaceNodes(newItems.Keys, (node1, node2) => newItems[node1]);
                }

                return classNode;
            }
        }
    }
}