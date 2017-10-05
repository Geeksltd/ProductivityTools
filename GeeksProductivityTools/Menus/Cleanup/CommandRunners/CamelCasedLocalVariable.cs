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

        abstract class Renamer
        {
            SemanticModel _semanticModel;
            public Document Document { get; private set; }

            public Renamer(Document document)
            {
                Document = document;
                _semanticModel = Document.GetSemanticModelAsync().Result;
            }

            public SyntaxNode RenameDeclarations(SyntaxNode StartNode)
            {
                SyntaxNode currentNode;
                SyntaxNode newNode = StartNode;

                do
                {
                    currentNode = newNode;
                    newNode = RenameDeclarations(currentNode, internalGetItemsToRename(currentNode));
                }
                while (newNode != currentNode);

                return newNode;
            }

            IList<string> VisitedTokens = new List<string>();
            protected IList<KeyValuePair<SyntaxToken, string>> internalGetItemsToRename(SyntaxNode currentNode)
            {
                return
                    GetItemsToRename(currentNode)
                    .Select(
                        identifier =>
                            new KeyValuePair<SyntaxToken, string>
                            (
                                identifier,
                                GetNewName(identifier)
                            )
                    )
                    .Where(x => x.Value != null)
                    .Where(x => VisitedTokens.Contains(x.Value) == false)
                    .ToList();
            }
            protected abstract IEnumerable<SyntaxToken> GetItemsToRename(SyntaxNode currentNode);

            SyntaxNode RenameDeclarations(SyntaxNode method, IList<KeyValuePair<SyntaxToken, string>> nodesIdentifiers)
            {
                foreach (var identifier in nodesIdentifiers)
                {
                    var newMethod = RenameDeclarations(method, identifier);

                    VisitedTokens.Add(identifier.Value);

                    if (newMethod != method)
                    {

                        return newMethod;
                    }
                }

                return method;
            }
            SyntaxNode RenameDeclarations(SyntaxNode methodNode, KeyValuePair<SyntaxToken, string> identifierToken)
            {
                var declarationNode = identifierToken.Key.Parent;

                var variableSymbol = _semanticModel.GetDeclaredSymbol(declarationNode);

                var newVarName = identifierToken.Value;

                var validateNameResult = RenameHelper.IsValidNewMemberNameAsync(_semanticModel, variableSymbol, newVarName).Result;

                if (validateNameResult == false) return methodNode;

                var result = RenameHelper.RenameSymbol(Document, Document.GetSyntaxRootAsync().Result, methodNode, declarationNode, newVarName);

                methodNode = result.Node;
                Document = result.Document;
                _semanticModel = result.Document.GetSemanticModelAsync().Result;
                return methodNode;
            }
            protected string GetNewName(SyntaxToken identifierToken)
            {
                var variableName = identifierToken.ValueText;

                var noneLetterCount = variableName.TakeWhile(x => Char.IsLetter(x) == false).Count();
                if (noneLetterCount >= variableName.Length) return null;
                if (char.IsUpper(variableName[noneLetterCount]) == false) return null;

                var newVarName =
                    variableName.Substring(0, noneLetterCount) +
                    variableName.Substring(noneLetterCount, 1).ToLower() +
                    variableName.Substring(noneLetterCount + 1);

                if (ValidateNewName(newVarName) == false) return null;

                return newVarName;
            }

            const string KEYWORD = "Keyword";
            Lazy<string[]> keywords =
                new Lazy<string[]>(
                () =>
                    Enum.GetNames(typeof(SyntaxKind))
                        .Where(k => k.EndsWith(KEYWORD))
                        .Select(k => k.Remove(k.Length - KEYWORD.Length).ToLower())
                        .ToArray()
                );

            protected virtual bool ValidateNewName(string newVarName)
            {
                bool isValid = true;
                isValid &= SyntaxFacts.IsValidIdentifier(newVarName);
                isValid &= !keywords.Value.Contains(newVarName);
                return isValid;
            }
        }

        class VariableRenamer : Renamer
        {
            public VariableRenamer(Document document) : base(document)
            {
            }

            protected override IEnumerable<SyntaxToken> GetItemsToRename(SyntaxNode currentNode)
            {
                return
                    currentNode
                    .DescendantNodes()
                    .OfType<VariableDeclaratorSyntax>()
                    .Select(x => x.Identifier);
            }
        }
        class ParameterRenamer : Renamer
        {
            public ParameterRenamer(Document document) : base(document)
            {
            }

            protected override IEnumerable<SyntaxToken> GetItemsToRename(SyntaxNode currentNode)
            {
                return
                    (currentNode as MethodDeclarationSyntax)
                    .ParameterList
                    .Parameters
                    .Select(x => x.Identifier);
            }
        }
        class FieldRenamer : Renamer
        {
            public FieldRenamer(Document document) : base(document)
            {
            }

            protected override IEnumerable<SyntaxToken> GetItemsToRename(SyntaxNode currentNode)
            {
                List<VariableDeclaratorSyntax> output = new List<VariableDeclaratorSyntax>();
                foreach (var item in (currentNode as ClassDeclarationSyntax)
                                        .Members.OfType<FieldDeclarationSyntax>()
                                        .Where(x=>x.Modifiers.Any(m=>m.IsKind(SyntaxKind.PrivateKeyword))))
                {
                    output.AddRange(item.Declaration.Variables);
                }

                return output.Select(x => x.Identifier);

            }
        }
    }
}