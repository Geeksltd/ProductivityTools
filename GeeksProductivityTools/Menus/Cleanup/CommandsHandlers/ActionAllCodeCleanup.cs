using Microsoft.VisualStudio.Shell;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public class ActionAllCodeCleanup : ExtendedBaseCodeCleanupCommand
    {
        public ActionAllCodeCleanup(OleMenuCommandService menu)
            : base(menu, PkgCmdIDList.CmdCleanUpAllActions, Definition.CodeCleanerType.All)
        { }
    }
}
