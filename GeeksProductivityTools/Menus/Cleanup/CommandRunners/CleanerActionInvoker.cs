using System.Threading.Tasks;
using EnvDTE;
using Geeks.GeeksProductivityTools.Definition;
using Microsoft.CodeAnalysis;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public class CleanerActionInvoker
    {
        readonly ProjectItem Item;
        public CleanerActionInvoker(ProjectItem item) { Item = item; }

        public void InvokeAll()
        {
            var organizeUsingDirectiveTask = Task.Run(() => Invoke(CodeCleanerType.OrganizeUsingDirectives));
            var whiteSpaceNormalizerTask = organizeUsingDirectiveTask.ContinueWith(antecedentTask => Invoke(CodeCleanerType.NormalizeWhiteSpaces));
            var privateModifiersRemoverTask = whiteSpaceNormalizerTask.ContinueWith(antecedentTask => Invoke(CodeCleanerType.PrivateAccessModifier));
            var membersToExpressionBodyTask = privateModifiersRemoverTask.ContinueWith(antecedentTask => Invoke(CodeCleanerType.ConvertMembersToExpressionBodied));
            var fullNameTypesToBuiltInTypesTask = membersToExpressionBodyTask.ContinueWith(antecedentTask => Invoke(CodeCleanerType.ConvertFullNameTypesToBuiltInTypes));
            var simplyAsyncCallsCommandTask = fullNameTypesToBuiltInTypesTask.ContinueWith(antecedentTask => Invoke(CodeCleanerType.SimplyAsyncCallsCommand));
            var sortClassMembersCommandTask = simplyAsyncCallsCommandTask.ContinueWith(antecedentTask => Invoke(CodeCleanerType.SortClassMembersCommand));
            var simplifyClassFieldDeclarationsCommandTask = sortClassMembersCommandTask.ContinueWith(antecedentTask => Invoke(CodeCleanerType.SimplifyClassFieldDeclarationsCommand));

            Task.WaitAll(new[]
            {
                simplifyClassFieldDeclarationsCommandTask,
                sortClassMembersCommandTask,
                simplyAsyncCallsCommandTask,
                fullNameTypesToBuiltInTypesTask,
                membersToExpressionBodyTask,
                privateModifiersRemoverTask,
                whiteSpaceNormalizerTask,
                organizeUsingDirectiveTask
            });
        }

        public void Invoke(CodeCleanerType cleanerType)
        {
            var instance = CodeCleanerFactory.Create(cleanerType);
            new CodeCleaner(instance, Item).Run();
        }
        //TODO: By Alireza =>  To return Syntax node and pass syntaxNode no next clean up function and dont close windows for each cleanup , just for something like organize usings
        //public SyntaxNode InvokeInternal(CodeCleanerType cleanerType)
        //{
        //    var instance = CodeCleanerFactory.Create(cleanerType);
        //    var cleaner = new CodeCleaner(instance, Item);
        //    if (cleaner.Cleaner is CodeCleanerCommandRunnerBase)
        //        return (cleaner.Cleaner as CodeCleanerCommandRunnerBase).CleanUp(Item.ToSyntaxNode());

        //    cleaner.Run();
        //    return null;
        //}
    }
}
