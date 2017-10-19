using EnvDTE;
using Geeks.GeeksProductivityTools.Extensions;
using static Geeks.GeeksProductivityTools.Menus.ActionsOnCSharp.CSharpActionDelegate;

namespace Geeks.GeeksProductivityTools.Menus.ActionsOnCSharp
{
    public class ActionCSharpOnProjectItem
    {
        public static void Action(ProjectItem item, TargetAction targetAction, Definition.CodeCleanerType[] type)
        {
            if (item.IsCsharpFile() && !item.IsCSharpDesignerFile()) targetAction(item, type);

            if (item.ProjectItems == null) return;

            for (var i = 1; i <= item.ProjectItems.Count; i++)
                Action(item.ProjectItems.Item(i), targetAction, type);
        }
    }
}
