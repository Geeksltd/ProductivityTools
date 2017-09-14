using Microsoft.VisualStudio.Shell;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public class ConvertMembersToExpressionBodiedCommand : ExtendedBaseCodeCleanupCommand
    {
        public ConvertMembersToExpressionBodiedCommand(OleMenuCommandService menu)
            : base(menu, PkgCmdIDList.CmdConvertMembersToExpressionBodied, Definition.CodeCleanerType.ConvertMembersToExpressionBodied)
        { }

    }
}
