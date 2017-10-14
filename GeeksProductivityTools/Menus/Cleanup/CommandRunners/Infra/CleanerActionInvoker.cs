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
            var organizeUsingDirectiveTask = Task.Run(() => Invoke(CodeCleanerType.OrganizeUsingDirectives));
            var privateModifiersRemoverTask = organizeUsingDirectiveTask.ContinueWith(antecedentTask => Invoke(CodeCleanerType.PrivateAccessModifier));
            var membersToExpressionBodyTask = privateModifiersRemoverTask.ContinueWith(antecedentTask => Invoke(CodeCleanerType.ConvertMembersToExpressionBodied));
            var fullNameTypesToBuiltInTypesTask = membersToExpressionBodyTask.ContinueWith(antecedentTask => Invoke(CodeCleanerType.ConvertFullNameTypesToBuiltInTypes));
            var simplyAsyncCallsCommandTask = fullNameTypesToBuiltInTypesTask.ContinueWith(antecedentTask => Invoke(CodeCleanerType.SimplyAsyncCallsCommand));
            var sortClassMembersCommandTask = simplyAsyncCallsCommandTask.ContinueWith(antecedentTask => Invoke(CodeCleanerType.SortClassMembersCommand));
            var simplifyClassFieldDeclarationsCommandTask = sortClassMembersCommandTask.ContinueWith(antecedentTask => Invoke(CodeCleanerType.SimplifyClassFieldDeclarationsCommand));
            var RemoveAttributeKeyworkCommandTask = simplifyClassFieldDeclarationsCommandTask.ContinueWith(antecedentTask => Invoke(CodeCleanerType.RemoveAttributeKeyworkCommand));
            var CompactSmallIfElseStatementsCommandTask = RemoveAttributeKeyworkCommandTask.ContinueWith(antecedentTask => Invoke(CodeCleanerType.CompactSmallIfElseStatementsCommand));
            var RemoveExtraThisQualificationCommandTask = CompactSmallIfElseStatementsCommandTask.ContinueWith(antecedentTask => Invoke(CodeCleanerType.RemoveExtraThisQualification));
            var CamelCasedLocalVariableTask = RemoveExtraThisQualificationCommandTask.ContinueWith(antecedentTask => Invoke(CodeCleanerType.CamelCasedLocalVariable));
            var CamelCasedFieldsTask = CamelCasedLocalVariableTask.ContinueWith(antecedentTask => Invoke(CodeCleanerType.CamelCasedFields));
            var CamelCasedConstFieldsTask = CamelCasedFieldsTask.ContinueWith(antecedentTask => Invoke(CodeCleanerType.CamelCasedConstFields));
            var whiteSpaceNormalizerTask = CamelCasedConstFieldsTask.ContinueWith(antecedentTask => Invoke(CodeCleanerType.NormalizeWhiteSpaces));

            Task.WaitAll(new[]
            {
                whiteSpaceNormalizerTask,
                CamelCasedConstFieldsTask,
                CamelCasedFieldsTask,
                CamelCasedLocalVariableTask,
                RemoveExtraThisQualificationCommandTask,
                CompactSmallIfElseStatementsCommandTask,
                RemoveAttributeKeyworkCommandTask,
                simplifyClassFieldDeclarationsCommandTask,
                sortClassMembersCommandTask,
                simplyAsyncCallsCommandTask,
                fullNameTypesToBuiltInTypesTask,
                membersToExpressionBodyTask,
                privateModifiersRemoverTask,
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
