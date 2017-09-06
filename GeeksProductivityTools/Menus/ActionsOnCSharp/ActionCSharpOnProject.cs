using System;
using EnvDTE;
using Geeks.GeeksProductivityTools.Utils;
using static Geeks.GeeksProductivityTools.Menus.ActionsOnCSharp.CSharpActionDelegate;

namespace Geeks.GeeksProductivityTools.Menus.ActionsOnCSharp
{
    public class ActionCSharpOnProject
    {
        public static void Invoke(TargetAction action, Definition.CodeCleanerType type)
        {
            try
            {
                var projects = DteServiceProvider.Instance.ActiveSolutionProjects as Array;
                var currentProject = projects.GetValue(0) as Project;

                if (currentProject.ProjectItems == null) return;

                for (var i = 1; i <= currentProject.ProjectItems.Count; i++)
                    ActionCSharpOnProjectItem.Action(currentProject.ProjectItems.Item(i), action, type);
            }
            catch (Exception e)
            {
                ErrorNotification.EmailError(e);
                ProcessActions.GeeksProductivityToolsProcess();
            }
        }
    }
}
