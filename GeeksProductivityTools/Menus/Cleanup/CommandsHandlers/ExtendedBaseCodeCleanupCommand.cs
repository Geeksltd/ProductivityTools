using System;
using System.ComponentModel.Design;
using System.Windows.Forms;
using Geeks.GeeksProductivityTools.Definition;
using Geeks.GeeksProductivityTools.Menus.ActionsOnCSharp;
using Geeks.GeeksProductivityTools.Utility;
using Microsoft.VisualStudio.Shell;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public abstract class ExtendedBaseCodeCleanupCommand : BaseCodeCleanupCommand
    {
        protected OleMenuCommandService Menu { get; private set; }
        protected uint CommandID { get; private set; }
        protected CodeCleanerType CleanerType { get; private set; }

        protected ExtendedBaseCodeCleanupCommand(OleMenuCommandService menu, uint commandID, CodeCleanerType cleanerType)
        {
            Menu = menu;
            CommandID = commandID;
            CleanerType = cleanerType;
        }
        public override void SetupCommands()
        {
            var menuCommandID = new CommandID(GuidList.GuidCleanupCmdSet, (int)CommandID);
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

            CSharpActionDelegate.TargetAction desiredAction = ActionsCSharpOnFile.DoCleanup;
            var commandGuid = (sender as OleMenuCommand).CommandID.Guid;

            if (commandGuid == GuidList.GuidCleanupCmdSet)
                ActionCSharpOnProject.Invoke(desiredAction, CleanerType);
            else return;
        }
    }
}
