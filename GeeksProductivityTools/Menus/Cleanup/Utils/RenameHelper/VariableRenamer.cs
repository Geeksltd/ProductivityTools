using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    class VariableRenamer : Renamer
    {
        public VariableRenamer(Document document) : base(document)
        {
        }

        protected override IEnumerable<SyntaxToken> GetItemsToRename(SyntaxNode currentNode)
        {
            return
                currentNode
                .DescendantNodes()
                .OfType<VariableDeclaratorSyntax>()
                .Select(x => x.Identifier);
        }

        protected override string GetNewName(string currentName) => GetCamelCased(currentName);
    }
}
