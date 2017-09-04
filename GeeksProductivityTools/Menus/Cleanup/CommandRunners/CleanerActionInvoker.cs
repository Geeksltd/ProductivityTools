using EnvDTE;
using Geeks.GeeksProductivityTools.Definition;
using System.Threading.Tasks;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public class CleanerActionInvoker
    {
        readonly ProjectItem Item;
        public CleanerActionInvoker(ProjectItem item) { Item = item; }

        public void InvokeAll()
        {
            var organizeUsingDirectiveTask = Task.Run(() => InvokeUsingDirectiveOrganizer());
            var whiteSpaceNormalizerTask = organizeUsingDirectiveTask.ContinueWith(antecedentTask => InvokeWhiteSpaceNormalizer());
            var privateModifiersRemoverTask = whiteSpaceNormalizerTask.ContinueWith(antecedentTask => InvokePrivateModifierRemover());

            //var privateModifiersRemoverTask = Task.Run(() => InvokePrivateModifierRemover());
            //var whiteSpaceNormalizerTask = privateModifiersRemoverTask.ContinueWith(antecedentTask => InvokeWhiteSpaceNormalizer());
            //var organizeUsingDirectiveTask = whiteSpaceNormalizerTask.ContinueWith(antecedentTask => InvokeUsingDirectiveOrganizer());

            Task.WaitAll(new[] { whiteSpaceNormalizerTask, privateModifiersRemoverTask, organizeUsingDirectiveTask });
        }

        public void InvokePrivateModifierRemover()
        {
            var instance = CodeCleanerFactory.Create(CodeCleanerType.PrivateAccessModifier);
            new CodeCleaner(instance, Item).Run();
        }

        public void InvokeWhiteSpaceNormalizer()
        {
            var instance = CodeCleanerFactory.Create(CodeCleanerType.NormalizeWhiteSpaces);
            new CodeCleaner(instance, Item).Run();
        }

        public void InvokeUsingDirectiveOrganizer()
        {
            var instance = CodeCleanerFactory.Create(CodeCleanerType.OrganizeUsingDirectives);
            new CodeCleaner(instance, Item).Run();
        }
    }
}
