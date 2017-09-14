using Microsoft.VisualStudio.Shell;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public class SimplyAsyncCallsCommand : ExtendedBaseCodeCleanupCommand
    {
        public SimplyAsyncCallsCommand(OleMenuCommandService menu)
            : base(menu, PkgCmdIDList.CmdSimplyAsyncCalls, Definition.CodeCleanerType.SimplyAsyncCallsCommand)
        {
        }
    }
}