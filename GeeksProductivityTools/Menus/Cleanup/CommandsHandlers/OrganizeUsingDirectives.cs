using Microsoft.VisualStudio.Shell;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public class OrganizeUsingDirectives : ExtendedBaseCodeCleanupCommand
    {
        public OrganizeUsingDirectives(OleMenuCommandService menu)
            : base(menu, PkgCmdIDList.CmdCleanUpOrganizeUsingDirectives, Definition.CodeCleanerType.OrganizeUsingDirectives)
        { }
    }
}
