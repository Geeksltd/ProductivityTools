using Microsoft.VisualStudio.Shell;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public class CompactSmallIfElseStatementsCommand : ExtendedBaseCodeCleanupCommand
    {
        public CompactSmallIfElseStatementsCommand(OleMenuCommandService menu)
            : base(menu, PkgCmdIDList.CmdCompactSmallIfElseStatements, Definition.CodeCleanerType.CompactSmallIfElseStatementsCommand)
        { }
    }
}
