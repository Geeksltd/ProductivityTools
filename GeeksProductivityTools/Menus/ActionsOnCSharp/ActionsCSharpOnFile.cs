using System;
using EnvDTE;
using Geeks.GeeksProductivityTools.Definition;
using Geeks.GeeksProductivityTools.Menus.Cleanup;
using Geeks.GeeksProductivityTools.Utils;

namespace Geeks.GeeksProductivityTools.Menus.ActionsOnCSharp
{
    public class ActionsCSharpOnFile
    {
        public static void DoCleanup(ProjectItem item, CodeCleanerType actionType)
        {
            try
            {
                var path = item.Properties.Item("FullPath").Value.ToString();
                if (path.EndsWithAny(new[] { "AssemblyInfo.cs", "TheApplication.cs" })) return;

                var window = item.Open(Constants.vsViewKindCode);

                window.Activate();
                CodeCleanerHost.Run(item, actionType);
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
