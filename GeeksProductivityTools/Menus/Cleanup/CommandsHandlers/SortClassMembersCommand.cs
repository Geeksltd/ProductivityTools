using Microsoft.VisualStudio.Shell;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public class SortClassMembersCommand : ExtendedBaseCodeCleanupCommand
    {
        public SortClassMembersCommand(OleMenuCommandService menu)
            : base(menu, PkgCmdIDList.CmdSortClassMembers, Definition.CodeCleanerType.SortClassMembersCommand)
        {
        }
    }
}