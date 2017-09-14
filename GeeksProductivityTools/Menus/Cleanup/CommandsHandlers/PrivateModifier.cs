using Microsoft.VisualStudio.Shell;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public class PrivateModifier : ExtendedBaseCodeCleanupCommand
    {
        public PrivateModifier(OleMenuCommandService menu)
            : base(menu, PkgCmdIDList.CmdCleanUpPrivateModifier, Definition.CodeCleanerType.PrivateAccessModifier)
        { }
    }
}
