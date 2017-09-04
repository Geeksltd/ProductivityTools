using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    // TODO: Refactor this bloody class Ali
    public class WhiteSpaceNormalizer : ICodeCleaner
    {
        const int MinLeftPadding = 4;
        const string WhiteSpaceTriviaText = "    ";

        public void Run(ProjectItem item) => Task.Run(() => NormalizeUsingDirectives(item));

        static void NormalizeUsingDirectives(ProjectItem item)
        {
            var file = item.ToFullPathPropertyValue();

            var initialSource = item.ToSyntaxNode();
            var source = initialSource;

            var directives = GetUsingDirectives(source);

            var longTrailingDirectives = directives.Where(d => d.HasTrailingTrivia && d.GetTrailingTrivia().ToFullString() != Environment.NewLine);
            if (longTrailingDirectives.Any()) source = StripOffTrailingTrivia(source, directives);

            var longLeadingDirective = directives.Where(d => d.HasLeadingTrivia && d.GetLeadingTrivia().ToFullString() != Environment.NewLine
                        && d.GetLeadingTrivia().ToFullString() != WhiteSpaceTriviaText);
            if (longLeadingDirective.Any()) source = StripOffLeadingTrivia(source, directives);

            source = InsertPaddingToLeadingTrivia(source);
            source = StripOffNameSpaceLeadingDistances(source);

            source = StripOffFirstClassLeadingDistances(source);

            if (string.Compare(initialSource.ToFullString(), source.ToFullString(), ignoreCase: false) == 0)
                return;

            source.WriteSourceTo(item.ToFullPathPropertyValue());
        }

        /// <summary>
        /// when directives nested in a namespace missed their left spaces through previous operations, then this method adds them back
        /// </summary>
        private static SyntaxNode InsertPaddingToLeadingTrivia(SyntaxNode source)
        {
            var dirs = FindNameSpaces(source).SelectMany(ns => ns.DescendantNodes().OfType<UsingDirectiveSyntax>().Where(d => !d.HasLeadingTrivia));
            if (!dirs.Any()) return source;

            return source.ReplaceNodes(dirs, (_, newNode) => newNode.WithLeadingTrivia(SyntaxFactory.ParseLeadingTrivia(string.Empty.PadLeft(MinLeftPadding))));
        }

        static SyntaxNode StripOffFirstClassLeadingDistances(SyntaxNode modifiedSource)
        {
            var firstNamespaceClass = modifiedSource.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();

            if (firstNamespaceClass == null) return modifiedSource;

            var leadingTriviaText = firstNamespaceClass.GetLeadingTrivia().ToFullString();

            if (leadingTriviaText == Environment.NewLine) return modifiedSource;
            if (leadingTriviaText == WhiteSpaceTriviaText) return modifiedSource;
            if (leadingTriviaText == (Environment.NewLine + WhiteSpaceTriviaText)) return modifiedSource;

            var minimumTriviaSpace = new StringBuilder().Append(Environment.NewLine)
                                                        .Append(string.Empty.PadLeft(MinLeftPadding))
                                                        .ToString();

            SyntaxTriviaList replacementLeadingTrivia;

            var comment = firstNamespaceClass.GetLeadingTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia));
            if (comment.ToFullString().HasValue()) return modifiedSource;
            //    if (comment != null)
            //{
            //    var replacement = new StringBuilder().Append(minimumTriviaSpace).Append(comment.ToFullString()).ToString();
            //    replacementLeadingTrivia = SyntaxFactory.ParseLeadingTrivia(replacement);
            //}

            //else
            //    replacementLeadingTrivia = SyntaxFactory.ParseLeadingTrivia(minimumTriviaSpace);

            replacementLeadingTrivia = SyntaxFactory.ParseLeadingTrivia(minimumTriviaSpace);
            modifiedSource = modifiedSource.ReplaceNode(firstNamespaceClass, firstNamespaceClass.WithLeadingTrivia(replacementLeadingTrivia));

            return modifiedSource;
        }

        /// <summary>
        /// If thre's more than one empty line between the last using directive and the namespace coming after that, 
        /// then this method normalizes the distance down into one single new line
        /// </summary>
        static SyntaxNode StripOffNameSpaceLeadingDistances(SyntaxNode modifiedSource)
        {
            var namespaces = FindNameSpaces(modifiedSource).Where(ns => ns.GetLeadingTrivia().ToFullString() != Environment.NewLine
                                                && ns.GetLeadingTrivia().ToFullString() != string.Empty);
            if (!namespaces.Any()) return modifiedSource;

            return modifiedSource.ReplaceNodes(namespaces, (_, newNode) => newNode.WithLeadingTrivia(SyntaxFactory.ParseLeadingTrivia(Environment.NewLine)));
        }

        static IEnumerable<UsingDirectiveSyntax> GetUsingDirectives(SyntaxNode syntaxNode)
        {
            return syntaxNode.DescendantNodes().OfType<UsingDirectiveSyntax>();
        }

        static IEnumerable<NamespaceDeclarationSyntax> FindNameSpaces(SyntaxNode modifiedSourceCode)
        {
            return modifiedSourceCode.DescendantNodes().OfType<NamespaceDeclarationSyntax>();
        }

        static SyntaxNode StripOffTrailingTrivia(SyntaxNode actualSourceCode, IEnumerable<UsingDirectiveSyntax> usingDirectives)
        {
            return actualSourceCode.ReplaceNodes(usingDirectives, (_, newNode) => newNode.WithoutTrailingTrivia());
        }

        static SyntaxNode StripOffLeadingTrivia(SyntaxNode actualSourceCode, IEnumerable<UsingDirectiveSyntax> usingDirectives)
        {
            return actualSourceCode.ReplaceNodes(usingDirectives, (_, newNode) => newNode.WithoutLeadingTrivia());
        }
    }
}
