using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System;
using System.Text;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    class CONSTRenamer : Renamer
    {
        public CONSTRenamer(Document document) : base(document)
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
                            x.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword)) &&
                            IsPrivate(x)
                    );

            foreach (var item in selectedFields)
            {
                output.AddRange(item.Declaration.Variables);
            }

            return output.Select(x => x.Identifier);
        }
        protected override string GetNewName(string currentName)
        {
            StringBuilder newNameBuilder = new StringBuilder();
            bool lastCharIsLowwer = false;
            foreach (var c in currentName)
            {
                if (Char.IsUpper(c))
                {
                    if (lastCharIsLowwer)
                    {
                        newNameBuilder.Append("_");
                    }
                }
                else
                {
                    lastCharIsLowwer = true;
                }
                newNameBuilder.Append(c);
            }

            if (string.Compare(currentName, currentName.ToUpper(), false) == 0) return null;

            return newNameBuilder.ToString().ToUpper();
        }
    }
}
