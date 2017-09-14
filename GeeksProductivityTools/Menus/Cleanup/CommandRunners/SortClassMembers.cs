using System;
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
    public class SortClassMembers : CodeCleanerCommandRunnerBase, ICodeCleaner
    {
        public override SyntaxNode CleanUp(SyntaxNode initialSourceNode)
        {
            return SortClassMembersHelper(initialSourceNode);
        }

        public static SyntaxNode SortClassMembersHelper(SyntaxNode initialSource)
        {
            var classes =
                initialSource
                .DescendantNodes()
                .Where(x => x is ClassDeclarationSyntax)
                .OfType<ClassDeclarationSyntax>();

            var newClassesDic = new Dictionary<ClassDeclarationSyntax, ClassDeclarationSyntax>();

            foreach (var classNode in classes)
            {
                var newClassNode = SortClassMemebersHelper(classNode);
                if (newClassNode == classNode) continue;
                newClassesDic.Add(classNode, newClassNode);
            }

            initialSource =
                initialSource
                    .ReplaceNodes
                    (
                        classes,
                        (oldNode1, oldNode2) => newClassesDic[oldNode1]
                    );

            return initialSource;
        }

        public static ClassDeclarationSyntax SortClassMemebersHelper(ClassDeclarationSyntax classNode)
        {
            var methods = classNode.DescendantNodes().Where(x => x is MethodDeclarationSyntax).ToList();
            var firstMethod = methods.FirstOrDefault();
            if (firstMethod == null) return classNode;

            var constructors = classNode.DescendantNodes().Where(x => x is ConstructorDeclarationSyntax).ToList();
            if (constructors.Any() == false) return classNode;

            SyntaxNode lastConstructoreWithGoodPosition = null;
            var constructorsToMoveList = new List<SyntaxNode>();

            foreach (var constructorItem in constructors)
            {
                if (firstMethod.SpanStart < constructorItem.SpanStart)
                {
                    constructorsToMoveList.Add(constructorItem);
                }
                else
                {
                    lastConstructoreWithGoodPosition = constructorItem;
                }
            }

            return
                classNode
                    .RemoveNodes(constructorsToMoveList, SyntaxRemoveOptions.KeepNoTrivia)
                    .InsertNodesAfter(lastConstructoreWithGoodPosition, constructorsToMoveList);
        }
    }
}