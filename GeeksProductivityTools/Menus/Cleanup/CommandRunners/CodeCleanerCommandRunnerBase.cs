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
    public abstract class CodeCleanerCommandRunnerBase : ICodeCleaner
    {
        public void Run(ProjectItem item) => Task.Run(() => AsyncRun(item));


        public ProjectItem ProjectItem { get; private set; }
        public string FilePath { get; private set; }
        protected virtual void AsyncRun(ProjectItem item)
        {
            FilePath = item.ToFullPathPropertyValue();
            ProjectItem = item;

            var initialSourceNode = item.ToSyntaxNode();

            initialSourceNode = CleanUp(initialSourceNode);

            initialSourceNode.WriteSourceTo(item.ToFullPathPropertyValue());
        }

        public abstract SyntaxNode CleanUp(SyntaxNode initialSourceNode);
    }
}