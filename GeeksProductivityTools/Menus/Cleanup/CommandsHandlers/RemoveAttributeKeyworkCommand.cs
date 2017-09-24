using Microsoft.VisualStudio.Shell;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public class RemoveAttributeKeyworkCommand : ExtendedBaseCodeCleanupCommand
    {
        public RemoveAttributeKeyworkCommand(OleMenuCommandService menu)
            : base(menu, PkgCmdIDList.CmdRemoveAttributeKeywork, Definition.CodeCleanerType.RemoveAttributeKeyworkCommand)
        { }
    }
}
