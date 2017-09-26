using EnvDTE;
using Microsoft.CodeAnalysis;
using RDocument = Microsoft.CodeAnalysis.Document;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public abstract class CodeCleanerCommandRunnerBase : ICodeCleaner
    {
        public void Run(ProjectItem item) => AsyncRun(item);

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

        SemanticModel _projectItemSemanticModel;
        public SemanticModel ProjectItemSemanticModel
        {
            get
            {
                if(_projectItemSemanticModel == null)
                {
                    _projectItemSemanticModel = ProjectItemDocument.GetSemanticModelAsync().Result;
                }
                return _projectItemSemanticModel;
            }
        }

        public string FilePath { get; private set; }

        protected virtual void AsyncRun(ProjectItem item)
        {
            FilePath = item.ToFullPathPropertyValue();
            ProjectItem = item;

            var initialSourceNode = ProjectItemDocument != null ? ProjectItemDocument.GetSyntaxRootAsync().Result : item.ToSyntaxNode();

            initialSourceNode = CleanUp(initialSourceNode);

            initialSourceNode.WriteSourceTo(item.ToFullPathPropertyValue());
        }

        public abstract SyntaxNode CleanUp(SyntaxNode initialSourceNode);
    }
}