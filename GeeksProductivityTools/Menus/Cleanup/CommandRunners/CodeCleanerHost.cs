using EnvDTE;
using Geeks.GeeksProductivityTools.Definition;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public class CodeCleanerHost
    {
        public static void Run(ProjectItem item, CodeCleanerType command)
        {
            if (!ActiveDocument.IsValid(item))
                ErrorNotification.EmailError(Resources.PrivateModifierCleanUpFailed);

            else
            {
                var invoker = new CleanerActionInvoker(item);
                switch (command)
                {
                    case CodeCleanerType.PrivateAccessModifier:
                    case CodeCleanerType.NormalizeWhiteSpaces:
                    case CodeCleanerType.ConvertMembersToExpressionBodied:
                    case CodeCleanerType.ConvertFullNameTypesToBuiltInTypes:
                    case CodeCleanerType.OrganizeUsingDirectives:
                    case CodeCleanerType.SimplyAsyncCallsCommand:
                    case CodeCleanerType.SortClassMembersCommand:
                    case CodeCleanerType.SimplifyClassFieldDeclarationsCommand:
                    case CodeCleanerType.RemoveAttributeKeyworkCommand:
                    case CodeCleanerType.CompactSmallIfElseStatementsCommand:
                    case CodeCleanerType.RemoveExtraThisQualification:
                    case CodeCleanerType.CamelCasedLocalVariable:
                        invoker.Invoke(command);
                        break;
                    case CodeCleanerType.All:
                        invoker.InvokeAll();
                        break;
                    default: break; // TODO
                }
            }
        }
    }
}
