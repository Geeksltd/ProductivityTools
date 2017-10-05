﻿using Microsoft.CodeAnalysis;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    abstract partial class Renamer
    {
        public class RenameOutput
        {
            public SyntaxNode Node { get; set; }
            public Solution Solution { get; set; }
            public Document Document { get; set; }
        }
    }
}
