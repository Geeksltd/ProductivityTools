
using Microsoft.VisualStudio.Shell;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public class CamelCasedLocalVariableCommand : ExtendedBaseCodeCleanupCommand
    {
        public CamelCasedLocalVariableCommand(OleMenuCommandService menu)
            : base(menu, PkgCmdIDList.CmdCamelCasedLocalVariable, Definition.CodeCleanerType.CamelCasedLocalVariable)
        { }
    }
}
