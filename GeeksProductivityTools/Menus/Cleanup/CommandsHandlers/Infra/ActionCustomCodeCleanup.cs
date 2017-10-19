using System;
using Microsoft.VisualStudio.Shell;
using Geeks.GeeksProductivityTools.Menus.Cleanup.CommandsHandlers.Infra;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public class ActionCustomCodeCleanup : BaseCodeCleanupCommand
    {
        public ActionCustomCodeCleanup(OleMenuCommandService menu)
            : base(menu, PkgCmdIDList.CmdCustomUpAllActions)
        { }

        protected override void CallBack(object sender, EventArgs e)
        {
            CleanupOptionForm.Instance.ShowDialog();

            ActionsOnCSharp.CSharpActionDelegate.TargetAction desiredAction = ActionsOnCSharp.ActionsCSharpOnFile.DoCleanup;

            if (CleanupOptionForm.Instance.SelectedTypes != null)
            {
                ActionsOnCSharp.ActionCSharpOnAnyWhere.Invoke(desiredAction, CleanupOptionForm.Instance.SelectedTypes);
                GeeksProductivityToolsPackage.Instance.SaveSolutionChanges();
            }
        }
    }
}
