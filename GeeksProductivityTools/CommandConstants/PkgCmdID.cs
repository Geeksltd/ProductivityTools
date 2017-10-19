// PkgCmdID.cs
// MUST match PkgCmdID.h

namespace Geeks.GeeksProductivityTools
{
    static class PkgCmdIDList
    {
        public const uint CmdidAttacher = 0x100;
        public const uint CmdidWebFileToggle = 0x101;
        public const uint CmdidFileFinder = 0x102;
        public const uint CmdidMemberFinder = 0x103;
        public const uint CmdidCSSFinder = 0x104;
        public const uint CmdidGotoNextFoundItem = 0x105;
        public const uint CmdidGotoPreviousFoundItem = 0x106;
        public const uint CmdidFixtureFileToggle = 0x107;

        public const uint CmdOpenInMSharp = 0x108;
        public const uint CmdOpenInMSharpAspx = 0x109;
        public const uint CmdRunBatchFile = 0x10a;
        public const uint CmdTrimBlankLines = 0x10b;
        public const uint CmdOrganizeUsing = 0x10c;
        public const uint CmdOrganizeUsingSlnLevel = 0x10d;
        public const uint CmdCompileTsFiles = 0x10e;
        public const uint CmdOpenInMSharpSln = 0x10f;

        public const uint CmdCustomUpAllActions = 0x0138;
    };
}