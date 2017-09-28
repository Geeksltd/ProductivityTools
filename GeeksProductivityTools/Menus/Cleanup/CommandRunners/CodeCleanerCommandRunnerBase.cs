using EnvDTE;
using Microsoft.CodeAnalysis;
using RDocument = Microsoft.CodeAnalysis.Document;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public abstract class CodeCleanerCommandRunnerBase : ICodeCleaner
    {
        public void Run(ProjectItem item) => AsyncRun(item);

        public ProjectItemDetailsType ProjectItemDetails { get; private set; }

        protected virtual void AsyncRun(ProjectItem item)
        {
            ProjectItemDetails = new ProjectItemDetailsType(item);

            var initialSourceNode = CleanUp(ProjectItemDetails.InitialSourceNode);

            initialSourceNode.WriteSourceTo(item.ToFullPathPropertyValue());
        }

        public abstract SyntaxNode CleanUp(SyntaxNode initialSourceNode);

        public class ProjectItemDetailsType
        {
            public ProjectItem ProjectItem { get; private set; }

            RDocument _projectItemDocument;
            public RDocument ProjectItemDocument
            {
                get
                {
                    if (_projectItemDocument == null)
                    {
                        _projectItemDocument = GeeksAddin.Utils.GetRoslynDomuentByProjectItem(ProjectItem);
                    }
                    return _projectItemDocument;
                }
            }

            SemanticModel _semanticModel;
            public SemanticModel SemanticModel
            {
                get
                {
                    if (_semanticModel == null)
                    {
                        _semanticModel = ProjectItemDocument.GetSemanticModelAsync().Result;
                    }
                    return _semanticModel;
                }
            }
            public SyntaxNode InitialSourceNode { get; private set; }

            public string FilePath { get; private set; }

            public ProjectItemDetailsType(ProjectItem item)
            {
                FilePath = item.ToFullPathPropertyValue();
                ProjectItem = item;
                InitialSourceNode = ProjectItemDocument != null ? ProjectItemDocument.GetSyntaxRootAsync().Result : item.ToSyntaxNode();
            }
        }
    }
}