using Microsoft.VisualStudio.Shell;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public class ConvertFullNameTypesToBuiltInTypesCommand : ExtendedBaseCodeCleanupCommand
    {
        public ConvertFullNameTypesToBuiltInTypesCommand(OleMenuCommandService menu)
            : base(menu, PkgCmdIDList.CmdConvertFullNameTypesToBuiltInTypes, Definition.CodeCleanerType.ConvertFullNameTypesToBuiltInTypes)
        { }
    }
}