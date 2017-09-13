using System.Threading.Tasks;
using EnvDTE;
using Geeks.GeeksProductivityTools.Definition;

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
            var membersToExpressionBodyTask = organizeUsingDirectiveTask.ContinueWith(antecedentTask => InvokeConvertMembersToExpressionBody());
            var fullNameTypesToBuiltInTypesTask = organizeUsingDirectiveTask.ContinueWith(antecedentTask => InvokeFullNameTypesToBuiltInTypes());

            //var privateModifiersRemoverTask = Task.Run(() => InvokePrivateModifierRemover());
            //var whiteSpaceNormalizerTask = privateModifiersRemoverTask.ContinueWith(antecedentTask => InvokeWhiteSpaceNormalizer());
            //var organizeUsingDirectiveTask = whiteSpaceNormalizerTask.ContinueWith(antecedentTask => InvokeUsingDirectiveOrganizer());

            Task.WaitAll(new[] { whiteSpaceNormalizerTask, membersToExpressionBodyTask, fullNameTypesToBuiltInTypesTask, privateModifiersRemoverTask, organizeUsingDirectiveTask });
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

        public void InvokeConvertMembersToExpressionBody()
        {
            var instance = CodeCleanerFactory.Create(CodeCleanerType.ConvertMembersToExpressionBodied);
            new CodeCleaner(instance, Item).Run();
        }
        public void InvokeFullNameTypesToBuiltInTypes()
        {
            var instance = CodeCleanerFactory.Create(CodeCleanerType.FullNameTypesToBuiltInTypes);
            new CodeCleaner(instance, Item).Run();
        }

        public void InvokeUsingDirectiveOrganizer()
        {
            var instance = CodeCleanerFactory.Create(CodeCleanerType.OrganizeUsingDirectives);
            new CodeCleaner(instance, Item).Run();
        }
    }
}
