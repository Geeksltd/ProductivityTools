using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public abstract class VariableRenamingBase : CodeCleanerCommandRunnerBase, ICodeCleaner
    {
        const string SELECTED_METHOD_ANNOTATION = "SELECTED_Node_To_RENAME_ANNOTATION";

        public override SyntaxNode CleanUp(SyntaxNode initialSourceNode)
        {
            SyntaxAnnotation annotationForSelectedNode = new SyntaxAnnotation(SELECTED_METHOD_ANNOTATION);
            var orginalDocument = ProjectItemDetails.ProjectItemDocument;
            var WorkingDocument = ProjectItemDetails.ProjectItemDocument;
            SyntaxNode workingNode;
            var annotatedRoot = initialSourceNode;
            do
            {
                workingNode = GetWorkingNode(annotatedRoot, annotationForSelectedNode);

                if (workingNode == null) continue;

                var annotatedNode = workingNode.WithAdditionalAnnotations(annotationForSelectedNode);
                annotatedRoot = annotatedRoot.ReplaceNode(workingNode, annotatedNode);
                WorkingDocument = WorkingDocument.WithSyntaxRoot(annotatedRoot);
                annotatedRoot = WorkingDocument.GetSyntaxRootAsync().Result;
                annotatedNode = annotatedRoot.GetAnnotatedNodes(annotationForSelectedNode).FirstOrDefault();

                var rewriter = GetRewriter(WorkingDocument);

                rewriter.Visit(annotatedNode);
                WorkingDocument = rewriter.WorkingDocument;

            } while (workingNode != null);

            if (SaveDocument(WorkingDocument, orginalDocument) == false)
            {
                return initialSourceNode;
            }

            return null;
        }

        bool SaveDocument(Document WorkingDocument, Document orginalDocument)
        {
            if (WorkingDocument != orginalDocument)
            {
                var changedProject = WorkingDocument.Project.GetChanges(orginalDocument.Project).GetChangedDocuments().ToList();

                if (changedProject.Count == 0) return false;

                foreach (var documentId in changedProject)
                {
                    var changedDocument = WorkingDocument.Project.GetDocument(documentId);

                    var changedDocumentRoot = changedDocument.GetSyntaxRootAsync().Result;

                    changedDocumentRoot.WriteSourceTo(changedDocument.FilePath);
                }
            }
            return true;
        }

        protected abstract SyntaxNode GetWorkingNode(SyntaxNode initialSourceNode, SyntaxAnnotation annotationForSelectedNodes);

        protected abstract VariableRenamingBaseRewriter GetRewriter(Document workingDocument);

        protected abstract class VariableRenamingBaseRewriter : CSharpSyntaxRewriter
        {
            public Document WorkingDocument { get; protected set; }

            public VariableRenamingBaseRewriter(Document workingDocument)
            {
                WorkingDocument = workingDocument;
            }
        }
    }
}