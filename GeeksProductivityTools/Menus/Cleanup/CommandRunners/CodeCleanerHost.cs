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
                        invoker.InvokePrivateModifierRemover();
                        break;
                    case CodeCleanerType.NormalizeWhiteSpaces:
                        invoker.InvokeWhiteSpaceNormalizer();
                        break;
                    case CodeCleanerType.OrganizeUsingDirectives:
                        invoker.InvokeUsingDirectiveOrganizer();
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
