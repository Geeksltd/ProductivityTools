using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System;
using Microsoft.CodeAnalysis.Rename;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public class RenameVariable : CodeCleanerCommandRunnerBase, ICodeCleaner
    {
        public override SyntaxNode CleanUp(SyntaxNode initialSourceNode)
        {
            return new Rewriter(ProjectItemDetails).Visit(initialSourceNode);
        }


        class Rewriter : CSharpSyntaxRewriter
        {
            private SemanticModel SemanticModel
            {
                get
                {
                    return projectItemDetails.SemanticModel;
                }
            }
            private ProjectItemDetailsType projectItemDetails;

            public Rewriter(ProjectItemDetailsType projectItemDetails)
            {
                this.projectItemDetails = projectItemDetails;
            }

            public override SyntaxNode VisitThisExpression(ThisExpressionSyntax thisNode)
            {
                if (thisNode.Parent is MemberAccessExpressionSyntax thisNodeMemberAccess)
                {
                    var right = thisNodeMemberAccess.Name;
                    var symbols = SemanticModel.LookupSymbols(thisNodeMemberAccess.SpanStart, name: right.Identifier.ValueText);
                    var thisItemAsMemberAccessExceptionSymbol = SemanticModel.GetSymbolInfo(thisNodeMemberAccess).Symbol;
                    if (symbols.Any(x => x == thisItemAsMemberAccessExceptionSymbol))
                    {
                        return right.WithLeadingTrivia(thisNodeMemberAccess.GetLeadingTrivia());
                    }
                }

                return base.VisitThisExpression(thisNode);
            }

            //public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
            //{
            //    return base.VisitClassDeclaration(Remove(node));
            //}

            //ClassDeclarationSyntax Remove(ClassDeclarationSyntax classNode)
            //{
            //    var thises = classNode.DescendantNodes().OfType<ThisExpressionSyntax>();
            //    var newItems = new Dictionary<MemberAccessExpressionSyntax, SyntaxNode>();

            //    foreach (var thisItem in thises)
            //    {
            //        if (thisItem.Parent is MemberAccessExpressionSyntax thisItemAsMemberAccessException)
            //        {
            //            var right = thisItemAsMemberAccessException.Name;
            //            var symbols = semanticModel.LookupSymbols(thisItemAsMemberAccessException.SpanStart, name: right.Identifier.ValueText);
            //            var thisItemAsMemberAccessExceptionSymbol = semanticModel.GetSymbolInfo(thisItemAsMemberAccessException).Symbol;
            //            if (symbols.Any(x => x == thisItemAsMemberAccessExceptionSymbol))
            //            {
            //                newItems.Add(thisItemAsMemberAccessException, right.WithLeadingTrivia(thisItemAsMemberAccessException.GetLeadingTrivia()));
            //            }
            //        }
            //    }

            //    if (newItems.Any())
            //    {
            //        classNode = classNode.ReplaceNodes(newItems.Keys, (node1, node2) => newItems[node1]);
            //    }

            //    return classNode;
            //}

            //public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
            //{
            //    return base.VisitMethodDeclaration(RenameVars(node));
            //}

            //private MethodDeclarationSyntax RenameVars(MethodDeclarationSyntax node)
            //{
            //    var vars = node.DescendantNodes().OfType<VariableDeclaratorSyntax>();
            //    foreach (var varItem in vars)
            //    {
            //        var variableName = varItem.Identifier.ValueText;
            //        var noneLetterCount = variableName.TakeWhile(x => Char.IsLetter(x) == false).Count();

            //        if (noneLetterCount < variableName.Length)
            //        {
            //            if (char.IsUpper(variableName[noneLetterCount]))
            //            {
            //                var newVarName =
            //                    variableName.Substring(0, noneLetterCount) +
            //                    variableName.Substring(noneLetterCount, 1).ToLower() +
            //                    variableName.Substring(noneLetterCount + 1);

            //                var option = removeExtraThis.ProjectItemDocument.Project.Solution.Workspace.Options;
            //                var tttttt =
            //                Renamer.RenameSymbolAsync(
            //                    removeExtraThis.ProjectItemDocument.Project.Solution,
            //                    semanticModel.GetSymbolInfo(varItem).Symbol,
            //                    newVarName,
            //                    option
            //                    ).Result.Projects.FirstOrDefault(x => x.Name == removeExtraThis.ProjectItemDocument.Project.Name)
            //                    .Documents.FirstOrDefault(x => x.Name == removeExtraThis.ProjectItemDocument.Name)
            //                    .GetSyntaxRootAsync().Result.ToFullString();
            //            }
            //        }
            //    }

            //    return node;
            //}
        }
    }
}