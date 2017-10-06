
using Microsoft.VisualStudio.Shell;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public class CamelCasedFieldsCommand : ExtendedBaseCodeCleanupCommand
    {
        public CamelCasedFieldsCommand(OleMenuCommandService menu)
            : base(menu, PkgCmdIDList.CmdCamelCasedFields, Definition.CodeCleanerType.CamelCasedFields)
        { }
    }
}
