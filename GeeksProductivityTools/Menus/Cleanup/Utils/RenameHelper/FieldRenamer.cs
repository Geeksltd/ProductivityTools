using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    class FieldRenamer : Renamer
    {
        public FieldRenamer(Document document) : base(document)
        {
        }

        protected override IEnumerable<SyntaxToken> GetItemsToRename(SyntaxNode currentNode)
        {
            List<VariableDeclaratorSyntax> output = new List<VariableDeclaratorSyntax>();

            var selectedFields =
                (currentNode as ClassDeclarationSyntax)
                    .Members.OfType<FieldDeclarationSyntax>()
                    .Where(
                        x =>
                            x.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)) ||
                            x.Modifiers
                                .Any(
                                    m =>
                                        m.IsKind(SyntaxKind.PublicKeyword) ||
                                        m.IsKind(SyntaxKind.ProtectedKeyword) ||
                                        m.IsKind(SyntaxKind.InternalKeyword)
                                ) == false
                    );

            foreach (var item in selectedFields)
            {
                output.AddRange(item.Declaration.Variables);
            }

            return output.Select(x => x.Identifier);

        }

        protected override string GetNewName(string currentName)
        {
            var camelCased = GetCamelCased(currentName);

            var noneLetterCount = camelCased.TakeWhile(x => x == '_').Count();

            camelCased = camelCased.Substring(noneLetterCount);

            return camelCased;
        }
    }
}
