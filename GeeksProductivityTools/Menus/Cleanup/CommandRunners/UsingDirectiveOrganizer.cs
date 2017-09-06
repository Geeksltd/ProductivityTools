using System;
using EnvDTE;
using Geeks.GeeksProductivityTools.Definition;
using Geeks.GeeksProductivityTools.Utils;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public class UsingDirectiveOrganizer : ICodeCleaner
    {
        public void Run(ProjectItem item)
        {
            try
            {
                var window = item.Open(Constants.vsViewKindCode);

                window.Activate();
                item.Document.DTE.ExecuteCommand(UsingsCommands.REMOVE_AND_SORT_COMMAND_NAME);
                window.Close(vsSaveChanges.vsSaveChangesYes);
            }
            catch (Exception e)
            {
                ErrorNotification.EmailError(e);
                ProcessActions.GeeksProductivityToolsProcess();
            }
        }
    }
}
