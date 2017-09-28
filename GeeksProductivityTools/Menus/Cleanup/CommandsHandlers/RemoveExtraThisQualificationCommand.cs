using Microsoft.VisualStudio.Shell;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public class RemoveExtraThisQualificationCommand : ExtendedBaseCodeCleanupCommand
    {
        public RemoveExtraThisQualificationCommand(OleMenuCommandService menu)
            : base(menu, PkgCmdIDList.CmdRemoveExtraThisQualification, Definition.CodeCleanerType.RemoveExtraThisQualification)
        { }
    }
}
