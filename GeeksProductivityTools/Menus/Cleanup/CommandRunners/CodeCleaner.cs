using EnvDTE;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public class CodeCleaner
    {
        ICodeCleaner Cleaner;
        ProjectItem Item;

        public CodeCleaner(ICodeCleaner cleaner, ProjectItem item)
        {
            Cleaner = cleaner;
            Item = item;
        }

        public void Run() => Cleaner.Run(Item);
    }
}
