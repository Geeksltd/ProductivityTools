using Microsoft.VisualStudio.Shell;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public class NormalizeWhiteSpaceCommand : ExtendedBaseCodeCleanupCommand
    {
        public NormalizeWhiteSpaceCommand(OleMenuCommandService menu)
            : base(menu, PkgCmdIDList.CmdCleanUpNormalizeWhiteSpaces, Definition.CodeCleanerType.NormalizeWhiteSpaces)
        { }
    }
}
