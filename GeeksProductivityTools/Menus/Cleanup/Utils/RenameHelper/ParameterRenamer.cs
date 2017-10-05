using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    class ParameterRenamer : Renamer
    {
        public ParameterRenamer(Document document) : base(document)
        {
        }

        protected override IEnumerable<SyntaxToken> GetItemsToRename(SyntaxNode currentNode)
        {
            return
                (currentNode as MethodDeclarationSyntax)
                .ParameterList
                .Parameters
                .Select(x => x.Identifier);
        }
        protected override string GetNewName(string currentName) => GetCamelCased(currentName);
    }
}
