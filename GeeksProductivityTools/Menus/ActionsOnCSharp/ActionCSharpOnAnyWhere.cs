using System;
using EnvDTE;
using Geeks.GeeksProductivityTools.Definition;
using Geeks.GeeksProductivityTools.Utils;
using static Geeks.GeeksProductivityTools.Menus.ActionsOnCSharp.CSharpActionDelegate;

namespace Geeks.GeeksProductivityTools.Menus.ActionsOnCSharp
{
    public class ActionCSharpOnAnyWhere
    {
        public static void Invoke(TargetAction action, Definition.CodeCleanerType type)
        {
            try
            {
                var projects = DteServiceProvider.Instance.ActiveSolutionProjects as Array;
                var ideSelectedItems = DteServiceProvider.Instance.SelectedItems;

                for (int itemIndex = 1; itemIndex <= ideSelectedItems.Count; itemIndex++)
                {
                    var selectedProjectItem = ideSelectedItems.Item(itemIndex).ProjectItem;

                    if (selectedProjectItem != null)
                    {
                        ActionCSharpOnProjectItem.Action(selectedProjectItem, action, type);
                    }
                    else
                    {
                        var selectedProject = ideSelectedItems.Item(itemIndex).Project;

                        if (selectedProject != null)
                        {
                            ActionCSharpOnProject.Invoke(action, type);
                            //for (int subItemIndex = 1; subItemIndex <= selectedProject.ProjectItems.Count; subItemIndex++)
                            //{
                            //    var subItem = selectedProject.ProjectItems.Item(subItemIndex);
                            //    ActionCSharpOnProjectItem.Action(subItem, action, type);
                            //}
                        }
                        else
                        {
                            ActionCSharpOnSolution.Invoke(action, type);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ErrorNotification.EmailError(e);
                ProcessActions.GeeksProductivityToolsProcess();
            }
        }

        private static void DoActionForItems(ProjectItems projectItems, TargetAction action, CodeCleanerType type)
        {
            for (int subItemIndex = 1; subItemIndex <= projectItems.Count; subItemIndex++)
            {
                var subItem = projectItems.Item(subItemIndex);
                ActionCSharpOnProjectItem.Action(subItem, action, type);
            }
        }
    }
}
