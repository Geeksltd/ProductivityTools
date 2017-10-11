using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using System.Collections.Generic;
using Geeks.GeeksProductivityTools.Menus.Cleanup.Renaming;
using System;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    abstract partial class Renamer
    {
        const string SELECTED_METHOD_ANNOTATION = "SELECTED_METHOD_ANNOTATION";
        private static RenameResult RenameSymbol(Document document, SyntaxNode root, SyntaxNode startNode, ParameterSyntax declarationNode, string newName)
        {
            var identifierToken = declarationNode.Identifier;

            var methodAnnotation = new SyntaxAnnotation(SELECTED_METHOD_ANNOTATION);
            var changeDic = new Dictionary<SyntaxNode, SyntaxNode>();
            changeDic.Add(startNode, startNode.WithAdditionalAnnotations(methodAnnotation));
            changeDic.Add(declarationNode, declarationNode.WithIdentifier(identifierToken.WithAdditionalAnnotations(RenameAnnotation.Create())));

            var annotatedRoot = root.ReplaceNodes(changeDic.Keys, (x, y) => changeDic[x]);

            var newSolution = RenameSymbol(document, annotatedRoot, identifierToken, methodAnnotation, newName).Result;

            return GetNewStartNode(newSolution, document, methodAnnotation, startNode);
        }
        private static RenameResult RenameSymbol(Document document, SyntaxNode root, SyntaxNode startNode, VariableDeclaratorSyntax declarationNode, string newName)
        {
            var identifierToken = declarationNode.Identifier;

            var methodAnnotation = new SyntaxAnnotation(SELECTED_METHOD_ANNOTATION);
            var changeDic = new Dictionary<SyntaxNode, SyntaxNode>();
            changeDic.Add(startNode, startNode.WithAdditionalAnnotations(methodAnnotation));
            changeDic.Add(declarationNode, declarationNode.WithIdentifier(identifierToken.WithAdditionalAnnotations(RenameAnnotation.Create())));

            var annotatedRoot = root.ReplaceNodes(changeDic.Keys, (x, y) => changeDic[x]);

            var newSolution = RenameSymbol(document, annotatedRoot, identifierToken, methodAnnotation, newName).Result;

            return GetNewStartNode(newSolution, document, methodAnnotation, startNode);
        }
        private static RenameResult GetNewStartNode(Solution newSolution, Document document, SyntaxAnnotation methodAnnotation, SyntaxNode startNode)
        {
            var newDocument =
               newSolution.Projects.FirstOrDefault(x => x.Name == document.Project.Name)
               .Documents.FirstOrDefault(x => x.FilePath == document.FilePath);

            var newRoot =
                newSolution.Projects.FirstOrDefault(x => x.Name == document.Project.Name)
                .Documents.FirstOrDefault(x => x.FilePath == document.FilePath).GetSyntaxRootAsync().Result;

            return new RenameResult
            {
                Node = newRoot.GetAnnotatedNodes(methodAnnotation).FirstOrDefault(),
                Solution = newSolution,
                Document = newDocument
            };
        }
        public static RenameResult RenameSymbol(Document document, SyntaxNode root, SyntaxNode startNode, SyntaxToken identifierToken, string newName)
        {
            return RenameSymbol(document, root, startNode, identifierToken.Parent, newName);
        }
        public static RenameResult RenameSymbol(Document document, SyntaxNode root, SyntaxNode startNode, SyntaxNode declarationNode, string newName)
        {
            if (declarationNode is VariableDeclaratorSyntax variableNode)
            {
                return RenameSymbol(document, root, startNode, variableNode, newName);
            }
            if (declarationNode is ParameterSyntax parameterNode)
            {
                return RenameSymbol(document, root, startNode, parameterNode, newName);
            }
            return null;
        }
        private static async Task<Solution> RenameSymbol(Document document, SyntaxNode annotatedRoot, SyntaxToken identifierNode, SyntaxAnnotation methodAnnotation, string newName, CancellationToken cancellationToken = default(CancellationToken))
        {
            var annotatedSolution = document.Project.Solution.WithDocumentSyntaxRoot(document.Id, annotatedRoot);
            var annotatedDocument = annotatedSolution.GetDocument(document.Id);
            annotatedRoot = await annotatedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var annotatedToken = annotatedRoot.FindToken(identifierNode.SpanStart);

            var semanticModel = await annotatedDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var symbol = semanticModel.GetDeclaredSymbol(annotatedToken.Parent, cancellationToken);

            var newSolution = await Microsoft.CodeAnalysis.Rename.Renamer.RenameSymbolAsync(annotatedSolution, symbol, newName, annotatedSolution.Workspace.Options, cancellationToken).ConfigureAwait(false);

            return newSolution;
        }

        SemanticModel _semanticModel;
        Document WorkingDocument { get; set; }

        public Renamer(Document document)
        {
            WorkingDocument = document;
            _semanticModel = WorkingDocument.GetSemanticModelAsync().Result;
        }

        public RenameResult RenameDeclarations(SyntaxNode containerNode)
        {
            SyntaxNode currentNode;
            SyntaxNode newNode = containerNode;
            RenameResult renamingResult = null;
            RenameResult workingRenamingResult = null;
            do
            {
                currentNode = newNode;
                workingRenamingResult = RenameDeclarations(currentNode, internalGetItemsToRename(currentNode));

                if (workingRenamingResult != null)
                {
                    renamingResult = workingRenamingResult;
                    newNode = renamingResult.Node;
                    WorkingDocument = renamingResult.Document;
                    _semanticModel = renamingResult.Document.GetSemanticModelAsync().Result;
                }
            }
            while (workingRenamingResult != null && newNode != currentNode);

            return renamingResult;
        }

        IList<string> VisitedTokens = new List<string>();
        protected IList<IdentifierStrcut> internalGetItemsToRename(SyntaxNode currentNode)
        {
            return
                GetItemsToRename(currentNode)
                .Select(
                    identifier =>
                        new IdentifierStrcut
                        {
                            Token = identifier,
                            NewName = GetNewNameWithChecking(identifier)
                        }
                )
                .Where(x => x.NewName != null)
                .Where(x => VisitedTokens.Contains(x.NewName) == false)
                .ToList();
        }
        protected abstract IEnumerable<SyntaxToken> GetItemsToRename(SyntaxNode currentNode);
        RenameResult RenameDeclarations(SyntaxNode containerNode, IList<IdentifierStrcut> identifieridentifierStrcuts)
        {
            foreach (var identifier in identifieridentifierStrcuts)
            {
                var newcontainerNode = RenameDeclarations(containerNode, identifier);
                VisitedTokens.Add(identifier.NewName);

                if (newcontainerNode != null)
                {
                    return newcontainerNode;
                }
            }

            return null;
        }
        RenameResult RenameDeclarations(SyntaxNode containerNode, IdentifierStrcut identifierStrcut)
        {
            var identifierDeclarationNode = identifierStrcut.Token.Parent;

            var identifierSymbol = _semanticModel.GetDeclaredSymbol(identifierDeclarationNode);

            var newVarName = identifierStrcut.NewName;

            var validateNameResult = RenameHelper.IsValidNewMemberNameAsync(_semanticModel, identifierSymbol, newVarName).Result;

            if (validateNameResult == false) return null;

            return RenameSymbol(WorkingDocument, WorkingDocument.GetSyntaxRootAsync().Result, containerNode, identifierDeclarationNode, newVarName);
        }
        string GetNewNameWithChecking(SyntaxToken identifierToken)
        {
            var variableName = identifierToken.ValueText;

            var newVarName = GetNewName(variableName);

            if (string.Compare(newVarName, variableName, false) == 0) return null;

            if (ValidateNewName(newVarName) == false) return null;

            return newVarName;
        }
        protected abstract string GetNewName(string currentName);

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

        protected static string GetCamelCased(string variableName)
        {
            var noneLetterCount = variableName.TakeWhile(x => Char.IsLetter(x) == false).Count();
            if (noneLetterCount >= variableName.Length) return variableName;
            if (char.IsUpper(variableName[noneLetterCount]) == false) return variableName;

            var newVarName =
                variableName.Substring(0, noneLetterCount) +
                variableName.Substring(noneLetterCount, 1).ToLower() +
                variableName.Substring(noneLetterCount + 1);

            return newVarName;
        }

        protected static bool IsPrivate(FieldDeclarationSyntax x)
        {
            return
                x.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)) ||
                x.Modifiers
                    .Any(
                        m =>
                            m.IsKind(SyntaxKind.PublicKeyword) ||
                            m.IsKind(SyntaxKind.ProtectedKeyword) ||
                            m.IsKind(SyntaxKind.InternalKeyword)
                    ) == false;
        }

        internal class IdentifierStrcut
        {
            public SyntaxToken Token { get; set; }
            public string NewName { get; set; }
        }
    }
}
