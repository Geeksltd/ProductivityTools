using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System;
using Microsoft.CodeAnalysis.Rename;
using Geeks.GeeksProductivityTools.Menus.Cleanup.Renaming;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public class CamelCasedLocalVariable : CodeCleanerCommandRunnerBase, ICodeCleaner
    {
        public override SyntaxNode CleanUp(SyntaxNode initialSourceNode)
        {
            return new Rewriter(ProjectItemDetails).Visit(initialSourceNode);
        }


        class Rewriter : CSharpSyntaxRewriter
        {
            private ProjectItemDetailsType projectItemDetails;

            public Document _document { get; private set; }

            public Rewriter(ProjectItemDetailsType projectItemDetails)
            {
                this.projectItemDetails = projectItemDetails;
                _document = this.projectItemDetails.ProjectItemDocument;
            }

            public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                return base.VisitClassDeclaration(RenameDeclarations(node) as ClassDeclarationSyntax);
            }
            public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                return base.VisitMethodDeclaration(RenameDeclarations(node) as MethodDeclarationSyntax);
            }

            MethodDeclarationSyntax RenameDeclarations(MethodDeclarationSyntax methodNode)
            {
                Renamer renamer = new VariableRenamer(_document);
                methodNode = renamer.RenameDeclarations(methodNode) as MethodDeclarationSyntax;
                _document = renamer.Document;
                methodNode = new ParameterRenamer(_document).RenameDeclarations(methodNode) as MethodDeclarationSyntax;
                return methodNode;
            }
            ClassDeclarationSyntax RenameDeclarations(ClassDeclarationSyntax classNode)
            {
                Renamer renamer = new FieldRenamer(_document);
                classNode = renamer.RenameDeclarations(classNode) as ClassDeclarationSyntax;
                _document = renamer.Document;
                return classNode;
            }
        }

    }
}