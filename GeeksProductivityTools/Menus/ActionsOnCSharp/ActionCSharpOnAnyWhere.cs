using System;
using EnvDTE;
using Geeks.GeeksProductivityTools.Definition;
using Geeks.GeeksProductivityTools.Utils;
using static Geeks.GeeksProductivityTools.Menus.ActionsOnCSharp.CSharpActionDelegate;

namespace Geeks.GeeksProductivityTools.Menus.ActionsOnCSharp
{
    public class ActionCSharpOnAnyWhere
    {
        public static void Invoke(TargetAction action, Definition.CodeCleanerType[] type)
        {
            try
            {
                var ideSelectedItems = DteServiceProvider.Instance.SelectedItems;

                for (int itemIndex = 1; itemIndex <= ideSelectedItems.Count; itemIndex++)
                {
                    var selectItem = ideSelectedItems.Item(itemIndex);

                    var selectedProjectItem = selectItem.ProjectItem;

                    if (selectedProjectItem != null)
                    {
                        if (selectedProjectItem.ProjectItems == null || selectedProjectItem.ProjectItems.Count == 0)
                        {
                            action(selectedProjectItem, type, true);
                        }
                        else
                        {
                            ActionCSharpOnProjectItem.Action(selectedProjectItem, action, type);
                        }
                    }
                    else if (selectItem.Project != null)
                    {
                        ActionCSharpOnProject.Invoke(action, type);
                    }
                    else
                    {
                        ActionCSharpOnSolution.Invoke(action, type);
                    }
                }
            }
            catch (Exception e)
            {
                ErrorNotification.EmailError(e);
                ProcessActions.GeeksProductivityToolsProcess();
            }
        }

        private static void DoActionForItems(ProjectItems projectItems, TargetAction action, CodeCleanerType[] type)
        {
            for (int subItemIndex = 1; subItemIndex <= projectItems.Count; subItemIndex++)
            {
                var subItem = projectItems.Item(subItemIndex);
                ActionCSharpOnProjectItem.Action(subItem, action, type);
            }
        }
    }
}
