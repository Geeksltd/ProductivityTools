using Microsoft.VisualStudio.Shell;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public class SimplifyClassFieldDeclarationsCommand : ExtendedBaseCodeCleanupCommand
    {
        public SimplifyClassFieldDeclarationsCommand(OleMenuCommandService menu)
            : base(menu, PkgCmdIDList.CmdSimplifyClassFieldDeclarations, Definition.CodeCleanerType.SimplifyClassFieldDeclarationsCommand)
        { }
    }
}
