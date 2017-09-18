using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public class ClassFieldsDefinition : CodeCleanerCommandRunnerBase, ICodeCleaner
    {
        public override SyntaxNode CleanUp(SyntaxNode initialSourceNode)
        {
            return ClassFieldsDefinitionHelper(initialSourceNode);
        }

        public static SyntaxNode ClassFieldsDefinitionHelper(SyntaxNode initialSourceNode)
        {
            initialSourceNode = new Rewriter(initialSourceNode).Visit(initialSourceNode);
            return initialSourceNode;
        }

        class Rewriter : CSharpSyntaxRewriter
        {
            public Rewriter(SyntaxNode initialSource) : base()
            {
            }

            public override SyntaxNode VisitVariableDeclarator(VariableDeclaratorSyntax node)
            {
                if (node.Initializer == null) return base.VisitVariableDeclarator(node);
                if (node.Parent is VariableDeclarationSyntax == false) return base.VisitVariableDeclarator(node);
                var variableTypeNode = (node.Parent as VariableDeclarationSyntax).Type;
                var value = node.Initializer.Value;

                if (value is LiteralExpressionSyntax)
                {
                    var valueObj = (value as LiteralExpressionSyntax).Token.Value;
                    var typeItem = TypesMapItem.GetAllPredefinedTypesDic()[(variableTypeNode as PredefinedTypeSyntax).Keyword.ValueText];

                    if (!valueObj.Equals(typeItem.DefaultValue)) return base.VisitVariableDeclarator(node);

                    node = node.WithInitializer(null).WithoutTrailingTrivia();
                }
                else if (value is DefaultExpressionSyntax)
                {
                    node = node.WithInitializer(null).WithoutTrailingTrivia();
                }
                else if (value is ObjectCreationExpressionSyntax)
                {
                    if (variableTypeNode.IsKind(SyntaxKind.PredefinedType))
                    {
                        node = node.WithInitializer(null).WithoutTrailingTrivia();
                    }
                }

                return base.VisitVariableDeclarator(node);
            }

        }
    }
}