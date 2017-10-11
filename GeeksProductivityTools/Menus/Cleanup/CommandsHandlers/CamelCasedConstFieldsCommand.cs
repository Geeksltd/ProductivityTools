
using Microsoft.VisualStudio.Shell;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public class CamelCasedConstFieldsCommand : ExtendedBaseCodeCleanupCommand
    {
        public CamelCasedConstFieldsCommand(OleMenuCommandService menu)
            : base(menu, PkgCmdIDList.CmdCamelCasedConstFields, Definition.CodeCleanerType.CamelCasedConstFields)
        { }
    }
}
