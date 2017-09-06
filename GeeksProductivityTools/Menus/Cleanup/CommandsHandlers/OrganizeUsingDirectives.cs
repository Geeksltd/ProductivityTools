using System;
using System.ComponentModel.Design;
using System.Windows.Forms;
using Geeks.GeeksProductivityTools.Menus.ActionsOnCSharp;
using Geeks.GeeksProductivityTools.Utility;
using Microsoft.VisualStudio.Shell;
using static Geeks.GeeksProductivityTools.Menus.ActionsOnCSharp.CSharpActionDelegate;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public class OrganizeUsingDirectives : BaseCodeCleanupCommand
    {
        OleMenuCommandService Menu;

        public OrganizeUsingDirectives(OleMenuCommandService menu) { Menu = menu; }

        public override void SetupCommands()
        {
            var menuCommandID = new CommandID(GuidList.GuidCleanupCmdSet, (int)PkgCmdIDList.CmdCleanUpOrganizeUsingDirectives);
            var menuItem = new OleMenuCommand(CallBack, menuCommandID);
            menuItem.BeforeQueryStatus += Item_BeforeQueryStatus;
            Menu.AddCommand(menuItem);
        }

        protected override void CallBack(object sender, EventArgs e)
        {
            var messageBoxResult = MessageBoxDisplay.Show(new MessageBoxDisplay.MessageBoxArgs
            {
                Message = Resources.WarnOnCodeCleanUp,
                Caption = Resources.WarningCaptionCleanup,
                Button = MessageBoxButtons.OKCancel,
                Icon = MessageBoxIcon.Warning
            });

            if (messageBoxResult != DialogResult.OK) return;

            TargetAction desiredAction = ActionsCSharpOnFile.DoCleanup;
            var commandGuid = (sender as OleMenuCommand).CommandID.Guid;

            if (commandGuid == GuidList.GuidCleanupCmdSet)
                ActionCSharpOnProject.Invoke(desiredAction, Definition.CodeCleanerType.OrganizeUsingDirectives);
            else return;
        }
    }
}
